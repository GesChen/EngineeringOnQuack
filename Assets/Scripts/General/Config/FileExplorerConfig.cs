using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class Config {
	public static class FileExplorer {

		public static readonly int MaxHistoryLength = 100;

		public static readonly float NavgationHeight	= 30;
		public static readonly float FooterItemsHeights	= 30;
		public static readonly float ItemHeight			= 30;
		public static readonly float IconNameSpacing	= 10;

		public static readonly string IconsFolder =
			Locations.IconsFolder + "File Explorer/";

		public static readonly string FileTypeIconsFolder =
			IconsFolder + "File Types/";

		public static readonly string FolderEntryIcon	= IconsFolder + "folder";

		public static readonly string BackIcon			= IconsFolder + "back";
		public static readonly string ForwardIcon		= IconsFolder + "forward";
		public static readonly string UpIcon			= IconsFolder + "up";
		public static readonly string RefreshIcon		= IconsFolder + "refresh";
		public static readonly string NewFolderIcon		= IconsFolder + "new folder";

		public static string GetFileIcon(string extension) =>
			FileTypeIconsFolder + extension switch {
				".txt"	=> "text",
				".json"	=> "code",
				".xml"	=> "code",
				".png"	=> "image",
				".jpg"	=> "image",
				".jpeg"	=> "image",
				".bmp"	=> "image",
				".gif"	=> "image",
				".tiff"	=> "image",
				".mp3"	=> "audio",
				".wav"	=> "audio",
				".ogg"	=> "audio",
				".mp4"	=> "video",
				".mov"	=> "video",
				".avi"	=> "video",
				".mkv"	=> "video",
				".zip"	=> "archive",
				".rar"	=> "archive",
				".7z"	=> "archive",
				_ => "defaultfile"
			};
	}
}