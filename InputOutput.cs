using Formula_1_Media_Handler.Properties;
using System.Text.RegularExpressions;

public class InputOutput
{

    /// <summary>Attempts to match for a valid torrent file within the provided <paramref name="path"/> directory.<br/>
    /// Returns the full path to the matching file if any are found. Returns <see langword="null"/> if no valid matches are found.</summary>
    /// <param name="path">Path to the directory where the torrent files are located.</param>
    public static string? FileMatch(string path)
    {
        var allowedExtensions = new[] { ".mkv", ".mp4", ".avi" };
        var files = Directory
            .GetFiles(path)
            .Where(file => allowedExtensions.Any(file.ToLower().EndsWith))
            .ToList();

        if (files.Count() == 1) {
            return files.First();
        } else {
            foreach (var f in files) {
                var fileName = new DirectoryInfo(f).Name;
                if (Regex.IsMatch(fileName, $@"02\..*\.Session", RegexOptions.IgnoreCase)) {
                    return f;
                }

                //if (Regex.IsMatch(fileName, @$"(02\..*\.Session)|({new DirectoryInfo(path).Name})", RegexOptions.IgnoreCase)) {
                //    return f;
                //}
            }
        }

        return null;
    }

    /// <summary>
    /// Returns 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string? ReturnCleanTitle(string name)
    {
        var ignoreCase = StringComparison.InvariantCultureIgnoreCase;

        if (name.Contains("Teds", ignoreCase)) { return null; }

        name = name.Replace(" one ", " 1 ", ignoreCase)
                   .Replace(" two ", " 2 ", ignoreCase)
                   .Replace(" three ", " 3 ", ignoreCase)
                   .Replace(" four ", " 4 ", ignoreCase)
                   .Replace(" five ", " 5 ", ignoreCase)
                   .Replace(".", " ")
                   .Replace("-", " ")
                   .Replace("_", " ");

        var regexRelease = "SkyF1HD|F1TV|1080p|720p";   //
        var title = Regex.Match(name, $@"\d+\s(\D+(\s\d)?)\s({regexRelease})", RegexOptions.IgnoreCase).Groups[1].Value;
        title = Regex.Replace(title.Trim(), @"\s+", " ");

        title = String.Join(" ", title.Split(' ').Where(x => !x.EndsWith("GP")));
        title = String.Join(" ", title.Split(' ').Distinct());

        return title;
    }

    /// <summary>Generates a destination path for an episode in the following format and returns it as <see cref="string"/>:<br/>
    /// <c>".\Formula 1\Season {<paramref name="season"/>}\Formula.1.S{<paramref name="season"/>}E{<paramref name="episode"/>}.{<paramref name="title"/>}.{<paramref name="fileEnd"/>}.{<paramref name="fileExt"/>}"</c></summary>
    /// <param name="title">Episode title/name.</param>
    /// <param name="season">Episode's season number.</param>
    /// <param name="episode">Episode's episode number.</param>
    /// <param name="fileEnd">Appends any custom string to the end of the file before the extension (ex. "1080p.AMZN.WEB-DL.x265").</param>
    /// <param name="fileExt">File extension to be used (ex. ".mkv" / "mkv").</param>
    public static string GenerateOutputPath(string title, string season, string episode, string fileEnd, string fileExt)
    {
        var fileName = $"Formula.1." +
            $"S{season}" +
            $"E{episode}" +
            $".{title}" +
            $".{fileEnd}" +
            $".{fileExt}";

        fileName = Regex.Replace(fileName.Trim(), @"\s+|\.+", ".");
        fileName = fileName.Replace("(", "")
                           .Replace(")", "");

        foreach (char c in Path.GetInvalidFileNameChars()) {
            fileName = fileName.Replace(c.ToString(), "");
        }

        var rootPath = Path.Combine(Settings.Default.Destination, $"Season {season}");
        var filePath = Path.Combine(rootPath, fileName);
        return filePath;
    }

    public class MediaFile
    {
        public string Season { get; set; } = string.Empty;
        public string Episode { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;

    }

    /// <summary>Recursively fetches all valid episode files from the root media directory.</summary>
    /// <remarks>Only returns the following filetypes: mkv, mp4, avi. Only returns files that contain a valid season/episode format (ex. 'S01E01').</remarks>
    /// <returns>A list of the MediaFile class for any matching files. MediaFile class includes the season, episode and path of the file.</returns>
    public static IEnumerable<MediaFile> FetchLibraryFiles()
    {
        var allowedExtensions = new[] { ".mkv", ".mp4", ".avi" };
        var mediaFiles = new List<MediaFile>();

        var files = Directory
            .GetFiles(Settings.Default.Destination, "*", searchOption: SearchOption.AllDirectories)
            .Where(file => allowedExtensions.Any(file.ToLower().EndsWith))
            .ToList();

        foreach (var f in files) {
            var matches = Regex.Match(f, @"S(\d{4})E(\d{2})");
            var currentFile = new MediaFile();
            currentFile.Season = matches.Groups[1].Value;
            currentFile.Episode = matches.Groups[2].Value;
            currentFile.Path = f;

            if (String.IsNullOrWhiteSpace(currentFile.Season) ||
                String.IsNullOrWhiteSpace(currentFile.Episode)) {

                continue;
            }

            mediaFiles.Add(currentFile);
        }

        return mediaFiles;
    }

    public static string NormalizePath(string path)
    {
        return Path.GetFullPath(new Uri(path).LocalPath)
                   .Replace("\"", "")
                   .Replace("\'", "")
                   .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   .ToUpperInvariant();
    }
}
