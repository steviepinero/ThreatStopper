using Microsoft.Extensions.Logging;
using System.Text;

namespace WindowsSecurityAgent.Core.Monitoring;

/// <summary>
/// Blocks URLs by modifying the Windows hosts file
/// </summary>
public class UrlBlocker
{
    private readonly ILogger<UrlBlocker> _logger;
    private readonly string _hostsFilePath;
    private const string MARKER_START = "# ThreatStopper - Blocked URLs - START";
    private const string MARKER_END = "# ThreatStopper - Blocked URLs - END";

    public UrlBlocker(ILogger<UrlBlocker> logger)
    {
        _logger = logger;
        _hostsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
    }

    /// <summary>
    /// Blocks a list of URLs/domains by redirecting them to 127.0.0.1
    /// </summary>
    public async Task BlockUrlsAsync(List<string> urlsToBlock)
    {
        try
        {
            _logger.LogInformation("Updating hosts file to block {Count} URLs", urlsToBlock.Count);

            // Read existing hosts file
            string[] existingLines;
            if (File.Exists(_hostsFilePath))
            {
                existingLines = await File.ReadAllLinesAsync(_hostsFilePath);
            }
            else
            {
                existingLines = Array.Empty<string>();
            }

            // Remove old blocked URLs section
            var newLines = new List<string>();
            bool inBlockedSection = false;

            foreach (var line in existingLines)
            {
                if (line.Trim() == MARKER_START)
                {
                    inBlockedSection = true;
                    continue;
                }
                if (line.Trim() == MARKER_END)
                {
                    inBlockedSection = false;
                    continue;
                }
                if (!inBlockedSection)
                {
                    newLines.Add(line);
                }
            }

            // Add new blocked URLs section
            if (urlsToBlock.Any())
            {
                newLines.Add("");
                newLines.Add(MARKER_START);
                newLines.Add($"# Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                // Extract and deduplicate domains
                var uniqueDomains = new HashSet<string>();
                foreach (var url in urlsToBlock)
                {
                    var domain = ExtractDomain(url);
                    if (!string.IsNullOrWhiteSpace(domain) && IsValidDomain(domain))
                    {
                        uniqueDomains.Add(domain);
                    }
                    else
                    {
                        _logger.LogWarning("Skipping invalid domain: {Url} (extracted as: {Domain})", url, domain ?? "(null)");
                    }
                }

                // Add blocked domains to hosts file
                foreach (var domain in uniqueDomains.OrderBy(d => d))
                {
                    newLines.Add($"127.0.0.1 {domain}");
                    newLines.Add($"127.0.0.1 www.{domain}");
                    _logger.LogInformation("Blocked domain: {Domain}", domain);
                }

                newLines.Add(MARKER_END);
                _logger.LogInformation("Added {Count} unique domains to hosts file", uniqueDomains.Count);
            }

            // Write back to hosts file
            await File.WriteAllLinesAsync(_hostsFilePath, newLines, Encoding.UTF8);

            // Flush DNS cache
            FlushDnsCache();

            _logger.LogInformation("Successfully updated hosts file with {Count} blocked URLs", urlsToBlock.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update hosts file");
            throw;
        }
    }

    /// <summary>
    /// Removes all blocked URLs from hosts file
    /// </summary>
    public async Task ClearBlockedUrlsAsync()
    {
        await BlockUrlsAsync(new List<string>());
    }

    /// <summary>
    /// Extracts domain from URL
    /// </summary>
    private string ExtractDomain(string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            // Normalize to lowercase first for case-insensitive processing
            url = url.Trim().ToLower();

            // Remove protocol if present (case-insensitive)
            if (url.StartsWith("http://"))
            {
                url = url.Substring(7); // Remove "http://"
            }
            else if (url.StartsWith("https://"))
            {
                url = url.Substring(8); // Remove "https://"
            }

            // Remove path if present
            var slashIndex = url.IndexOf('/');
            if (slashIndex > 0)
            {
                url = url.Substring(0, slashIndex);
            }

            // Remove port if present
            var colonIndex = url.IndexOf(':');
            if (colonIndex > 0)
            {
                url = url.Substring(0, colonIndex);
            }

            // Remove www. prefix if present (after protocol removal)
            if (url.StartsWith("www."))
            {
                url = url.Substring(4); // Remove "www."
            }

            return url.Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Validates if a string is a valid domain name
    /// </summary>
    private bool IsValidDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return false;

        // Basic validation: must contain at least one dot and be alphanumeric with dots and hyphens
        if (!domain.Contains('.'))
            return false;

        // Check for valid characters (letters, numbers, dots, hyphens)
        foreach (var c in domain)
        {
            if (!char.IsLetterOrDigit(c) && c != '.' && c != '-')
                return false;
        }

        // Must not start or end with dot or hyphen
        if (domain.StartsWith('.') || domain.EndsWith('.') || 
            domain.StartsWith('-') || domain.EndsWith('-'))
            return false;

        return true;
    }

    /// <summary>
    /// Flushes the DNS cache to apply changes immediately
    /// </summary>
    private void FlushDnsCache()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };
            process.Start();
            process.WaitForExit();

            _logger.LogInformation("DNS cache flushed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to flush DNS cache");
        }
    }

    /// <summary>
    /// Gets currently blocked URLs from hosts file
    /// </summary>
    public async Task<List<string>> GetBlockedUrlsAsync()
    {
        var blockedUrls = new List<string>();

        try
        {
            if (!File.Exists(_hostsFilePath))
                return blockedUrls;

            var lines = await File.ReadAllLinesAsync(_hostsFilePath);
            bool inBlockedSection = false;

            foreach (var line in lines)
            {
                if (line.Trim() == MARKER_START)
                {
                    inBlockedSection = true;
                    continue;
                }
                if (line.Trim() == MARKER_END)
                {
                    break;
                }
                if (inBlockedSection && line.StartsWith("127.0.0.1"))
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && !parts[1].StartsWith("www."))
                    {
                        blockedUrls.Add(parts[1]);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read blocked URLs from hosts file");
        }

        return blockedUrls;
    }
}

