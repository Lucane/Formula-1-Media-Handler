## Description

A console application for automatically downloading and managing media files for Formula 1 races.
The application designed to be run as a Windows Service in order to be as lightweight as possible.
The console interface is mainly used for setup and customization.

Formula 1 torrent files are notoriously unreliable in their naming convention and quality,
making PVR software such as [Sonarr](https://github.com/Sonarr/Sonarr) largely unusable for this purpose.

This application is not completely reliable as of yet, but works better than the alternatives I've found so far.
It was initially meant to be used in conjunction with [Plex](https://www.plex.tv/), but can also be used as a standalone application.

## Implementation

The application periodically checks if any races are missing and if so, searches and downloads the torrent through the qBittorrent WebUI API.

After that the application renames and imports the downloaded files to the specified media folder and removes any remnant torrent files.
Renaming is done using the season and episode metadata from [TheTVDB](https://thetvdb.com/series/formula-1).

Finding the correct match can be tricky, so I implemented several checks to determine whether the torrent is a match.
For example, one of those checks involves the use of the [Levenshtein distance](https://en.wikipedia.org/wiki/Levenshtein_distance) algorithm, which measures the edit distance between two strings.

## Console view

![Screenshot 2023-10-11 154847](https://github.com/Lucane/Formula-1-Media-Handler/assets/7999446/c7d090c1-2d69-46cc-a5a8-5f01871d561c)

![Screenshot 2023-10-11 155044](https://github.com/Lucane/Formula-1-Media-Handler/assets/7999446/a5649c99-e6f3-4cd7-bfa4-6e05229f62c8)

![Screenshot 2023-10-11 155208](https://github.com/Lucane/Formula-1-Media-Handler/assets/7999446/65c4343c-4379-417c-ab60-1aff33717a01)
