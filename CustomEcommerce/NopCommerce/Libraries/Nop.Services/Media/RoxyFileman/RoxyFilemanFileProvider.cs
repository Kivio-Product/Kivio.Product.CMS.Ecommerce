﻿using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Nop.Core.Domain.Media;
using Nop.Core.Infrastructure;
using SkiaSharp;

 namespace Nop.Services.Media.RoxyFileman; 

 /// <summary>
 /// Looks up and manages uploaded files using database through PictureService
 /// </summary>
 public partial class RoxyFilemanFileProvider : IRoxyFilemanFileProvider
 {
     #region Fields

     protected readonly INopFileProvider _nopFileProvider;
     protected readonly IPictureService _pictureService;
     protected readonly MediaSettings _mediaSettings;

     #endregion

     #region Ctor

     public RoxyFilemanFileProvider(INopFileProvider nopFileProvider, IPictureService pictureService, MediaSettings mediaSettings)
     {
         _nopFileProvider = nopFileProvider;
         _pictureService = pictureService;
         _mediaSettings = mediaSettings;
     }

     #endregion

     #region IFileProvider Implementation

     /// <summary>
     /// Get directory contents from database
     /// </summary>
     /// <param name="subpath">Directory path</param>
     /// <returns>Directory contents</returns>
     public IDirectoryContents GetDirectoryContents(string subpath)
     {
         // For database-based implementation, we'll return a minimal implementation
         // since RoxyFileman uses our custom GetFiles and GetDirectories methods
         return new NotFoundDirectoryContents();
     }

     /// <summary>
     /// Watch for changes - not applicable for database storage
     /// </summary>
     /// <param name="filter">Filter pattern</param>
     /// <returns>Change token</returns>
     public IChangeToken Watch(string filter)
     {
         // Database changes are not watched through file system watchers
         return NullChangeToken.Singleton;
     }

     #endregion

     #region Utilities

     /// <summary>
     /// Convert a virtual path to database-based path
     /// </summary>
     /// <param name="virtualPath">Virtual path</param>
     /// <returns>Database path representation</returns>
     protected virtual string VirtualPathToDatabasePath(string virtualPath)
     {
         if (string.IsNullOrEmpty(virtualPath))
             return string.Empty;

         // Normalize path separators and remove leading/trailing slashes
         return virtualPath.Replace('\\', '/').Trim('/');
     }

     /// <summary>
     /// Convert a database path to virtual path 
     /// </summary>
     /// <param name="databasePath">Database path</param>
     /// <returns>Virtual path representation</returns>
     protected virtual string DatabasePathToVirtualPath(string databasePath)
     {
         if (string.IsNullOrEmpty(databasePath))
             return string.Empty;

         return databasePath.Replace('/', Path.DirectorySeparatorChar);
     }

     /// <summary>
     /// Extract directory path from a file virtual path
     /// </summary>
     /// <param name="filePath">File virtual path</param>
     /// <returns>Directory path</returns>
     protected virtual string GetDirectoryPath(string filePath)
     {
         var directory = Path.GetDirectoryName(filePath);
         return string.IsNullOrEmpty(directory) ? string.Empty : directory.Replace('\\', '/');
     }

     /// <summary>
     /// Get pictures in a virtual directory
     /// </summary>
     /// <param name="virtualPath">Virtual directory path</param>
     /// <returns>Pictures in the directory</returns>
     protected virtual async Task<IList<Picture>> GetPicturesInDirectoryAsync(string virtualPath)
     {
         var databasePath = VirtualPathToDatabasePath(virtualPath);
         var pictures = await _pictureService.GetPicturesAsync(databasePath);
         return pictures.ToList();
     }

     /// <summary>
     /// Database-based file info implementation
     /// </summary>
     protected class DatabaseFileInfo : IFileInfo
     {
         private readonly Picture _picture;
         private readonly string _name;
         private readonly IPictureService _pictureService;

         public DatabaseFileInfo(string subpath, Picture picture, IPictureService pictureService = null)
         {
             _picture = picture;
             _name = Path.GetFileName(subpath);
             _pictureService = pictureService;
             PhysicalPath = subpath;
         }

         public bool Exists => _picture != null;
         public long Length => 0; // We don't track file size in Picture entity
         public string PhysicalPath { get; }
         public string Name => _name;
         public DateTimeOffset LastModified => DateTimeOffset.Now; // Could be enhanced with picture creation date
         public bool IsDirectory => false;

         public Stream CreateReadStream()
         {
             if (_picture == null)
                 throw new FileNotFoundException();

             if (_pictureService != null)
             {
                 try
                 {
                     var binaryData = _pictureService.LoadPictureBinaryAsync(_picture).Result;
                     return new MemoryStream(binaryData);
                 }
                 catch
                 {
                     // Fall back to empty stream if loading fails
                 }
             }

             return new MemoryStream();
         }
     }

     /// <summary>
     /// Adjust image measures to target size
     /// </summary>
     /// <param name="image">Source image</param>
     /// <param name="maxWidth">Target width</param>
     /// <param name="maxHeight">Target height</param>
     /// <returns>Adjusted width and height</returns>
     protected virtual (int width, int height) ValidateImageMeasures(SKBitmap image, int maxWidth = 0, int maxHeight = 0)
     {
         ArgumentNullException.ThrowIfNull(image);

         float width = Math.Min(image.Width, maxWidth);
         float height = Math.Min(image.Height, maxHeight);

         var targetSize = Math.Max(width, height);

         if (image.Height > image.Width)
         {
             // portrait
             width = image.Width * (targetSize / image.Height);
             height = targetSize;
         }
         else
         {
             // landscape or square
             width = targetSize;
             height = image.Height * (targetSize / image.Width);
         }

         return ((int)width, (int)height);
     }

     /// <summary>
     /// Get a file type by the specified path string
     /// </summary>
     /// <param name="subpath">The path string from which to get the file type</param>
     /// <returns>File type</returns>
     protected virtual string GetFileType(string subpath)
     {
         var fileExtension = Path.GetExtension(subpath)?.ToLowerInvariant();

         return fileExtension switch
         {
             ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".svg" => "image",
             ".swf" or ".flv" => "flash",
             ".mp4" or ".webm" or ".ogg" or ".mov" or ".m4a" or ".mp3" or ".wav" => "media",
             _ => "file"
         };

         /* These media extensions are not supported by HTML5 or tinyMCE out of the box
          * but may possibly be supported if You find players for them.
          * if (fileExtension == ".3gp" || fileExtension == ".flv" 
          *     || fileExtension == ".rmvb" || fileExtension == ".wmv" || fileExtension == ".divx"
          *     || fileExtension == ".divx" || fileExtension == ".mpg" || fileExtension == ".rmvb"
          *     || fileExtension == ".vob" // video
          *     || fileExtension == ".aif" || fileExtension == ".aiff" || fileExtension == ".amr"
          *     || fileExtension == ".asf" || fileExtension == ".asx" || fileExtension == ".wma"
          *     || fileExtension == ".mid" || fileExtension == ".mp2") // audio
          *     fileType = "media"; */
     }

     /// <summary>
     /// Get the virtual path for the specified path string in the database directory structure
     /// </summary>
     /// <param name="path">The file or directory path</param>
     /// <returns>The virtual path for database operations</returns>
     protected virtual string GetVirtualPath(string path)
     {
         if (string.IsNullOrEmpty(path))
             throw new RoxyFilemanException("NoFilesFound");

         path = path.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

         if (Path.IsPathRooted(path))
             throw new RoxyFilemanException("NoFilesFound");

         // Convert to database-friendly path
         var virtualPath = VirtualPathToDatabasePath(path);

         return virtualPath;
     }

     /// <summary>
     /// Get image format by mime type
     /// </summary>
     /// <param name="mimeType">Mime type</param>
     /// <returns>SKEncodedImageFormat</returns>
     protected virtual SKEncodedImageFormat GetImageFormatByMimeType(string mimeType)
     {
         var format = SKEncodedImageFormat.Jpeg;
         if (string.IsNullOrEmpty(mimeType))
             return format;

         var parts = mimeType.ToLowerInvariant().Split('/');
         var lastPart = parts[^1];

         switch (lastPart)
         {
             case "webp":
                 format = SKEncodedImageFormat.Webp;
                 break;
             case "png":
             case "gif":
             case "bmp":
             case "x-icon":
                 format = SKEncodedImageFormat.Png;
                 break;
             default:
                 break;
         }

         return format;
     }

     /// <summary>
     /// Get the unique name of the file (add -copy-(N) to the file name if there is already a file with that name in the directory)
     /// </summary>
     /// <param name="directoryPath">Path to the file directory</param>
     /// <param name="fileName">Original file name</param>
     /// <returns>Unique name of the file</returns>
     protected virtual string GetUniqueFileName(string directoryPath, string fileName)
     {
         var uniqueFileName = fileName;
         var baseFileName = Path.GetFileNameWithoutExtension(fileName);
         var extension = Path.GetExtension(fileName);

         var i = 0;
         while (true)
         {
             // Construct the full path properly for the lookup
             var searchPath = string.IsNullOrEmpty(directoryPath) 
                 ? uniqueFileName 
                 : $"{VirtualPathToDatabasePath(directoryPath)}/{uniqueFileName}";
                 
             if (FindPictureByPath(searchPath) == null)
                 break;
                 
             uniqueFileName = $"{baseFileName}-Copy-{++i}{extension}";
         }

         return uniqueFileName;
     }

     /// <summary>
     /// Check the specified path is valid for database operations
     /// </summary>
     /// <param name="virtualPath">The virtual path</param>
     /// <returns>True if path is valid; otherwise false</returns>
     protected virtual bool IsValidPath(string virtualPath)
     {
         return !string.IsNullOrEmpty(virtualPath) && 
                !Path.IsPathRooted(virtualPath) &&
                !virtualPath.Contains("..");
     }

     /// <summary>
     /// Scale image to fit the destination sizes
     /// </summary>
     /// <param name="data">Image data</param>
     /// <param name="format">SkiaSharp image format</param>
     /// <param name="maxWidth">Target width</param>
     /// <param name="maxHeight">Target height</param>
     /// <returns>The byte array of resized image</returns>
     protected virtual byte[] ResizeImage(byte[] data, SKEncodedImageFormat format, int maxWidth, int maxHeight)
     {
         using var sourceStream = new SKMemoryStream(data);
         using var inputData = SKData.Create(sourceStream);
         using var image = SKBitmap.Decode(inputData);

         var (width, height) = ValidateImageMeasures(image, maxWidth, maxHeight);

         var toBitmap = new SKBitmap(width, height, image.ColorType, image.AlphaType);

         if (!image.ScalePixels(toBitmap, SKFilterQuality.None))
             throw new Exception("Image scaling");

         var newImage = SKImage.FromBitmap(toBitmap);
         var imageData = newImage.Encode(format, _mediaSettings.DefaultImageQuality);

         newImage.Dispose();
         return imageData.ToArray();
     }

     #endregion

     #region Methods

     /// <summary>
     /// Moves a file or a directory and its contents to a new location in database
     /// </summary>
     /// <param name="sourceDirName">The path of the file or directory to move</param>
     /// <param name="destDirName">
     /// The path to the new location for sourceDirName. If sourceDirName is a file, then destDirName
     /// must also be a file name
     /// </param>
     public virtual void DirectoryMove(string sourceDirName, string destDirName)
     {
         if (destDirName.StartsWith(sourceDirName, StringComparison.InvariantCulture))
             throw new RoxyFilemanException("E_CannotMoveDirToChild");

         var sourcePath = VirtualPathToDatabasePath(sourceDirName);
         var destPath = VirtualPathToDatabasePath(destDirName);

         // Get all pictures in the source directory
         var pictures = _pictureService.GetPicturesAsync(sourcePath).Result;
         var picturesToMove = pictures.Where(p => IsInDirectory(p, sourcePath)).ToList();

         if (!picturesToMove.Any())
             throw new RoxyFilemanException("E_MoveDirInvalisPath");

         try
         {
             // Update the virtual path for all pictures in the directory
             foreach (var picture in picturesToMove)
             {
                 var newVirtualPath = picture.VirtualPath?.Replace(sourcePath, destPath);
                 picture.VirtualPath = newVirtualPath;
                 _pictureService.UpdatePictureAsync(picture).Wait();
             }
         }
         catch
         {
             throw new RoxyFilemanException("E_MoveDir");
         }
     }

     /// <summary>
     /// Locate a file at the given path by searching in the database.
     /// </summary>
     /// <param name="subpath">A path under the root directory</param>
     /// <returns>The file information. Caller must check Microsoft.Extensions.FileProviders.IFileInfo.Exists property.</returns>
     public IFileInfo GetFileInfo(string subpath)
     {
         if (string.IsNullOrEmpty(subpath))
             return new NotFoundFileInfo(subpath);

         subpath = subpath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

         // Absolute paths not permitted.
         if (Path.IsPathRooted(subpath))
             return new NotFoundFileInfo(subpath);

         // For database-based files, we create a virtual file info
         // This is a simplified implementation for the interface requirement
         try
         {
             var picture = FindPictureByPath(subpath);
             
             if (picture != null)
             {
                 return new DatabaseFileInfo(subpath, picture, _pictureService);
             }
         }
         catch
         {
             // Suppress exceptions and return not found
         }

         return new NotFoundFileInfo(subpath);
     }

     /// <summary>
     /// Create configuration file for RoxyFileman
     /// </summary>
     /// <param name="pathBase">The base path for the store</param>
     /// <param name="lang">Two-letter language code</param>
     /// <returns>A task that represents the asynchronous operation</returns>
     public virtual async Task<RoxyFilemanConfig> GetOrCreateConfigurationAsync(string pathBase, string lang)
     {
         //check whether the path base has changed, otherwise there is no need to overwrite the configuration file
         if (Singleton<RoxyFilemanConfig>.Instance?.RETURN_URL_PREFIX?.Equals(pathBase) ?? false)
         {
             return Singleton<RoxyFilemanConfig>.Instance;
         }

         var filePath = _nopFileProvider.GetAbsolutePath(NopRoxyFilemanDefaults.ConfigurationFile);

         //create file if not exists
         _nopFileProvider.CreateFile(filePath);

         //try to read existing configuration
         var existingText = await _nopFileProvider.ReadAllTextAsync(filePath, Encoding.UTF8);
         var existingConfiguration = JsonConvert.DeserializeObject<RoxyFilemanConfig>(existingText);

         //create configuration
         var configuration = new RoxyFilemanConfig
         {
             FILES_ROOT = existingConfiguration?.FILES_ROOT ?? NopRoxyFilemanDefaults.DefaultRootDirectory,
             SESSION_PATH_KEY = existingConfiguration?.SESSION_PATH_KEY ?? string.Empty,
             THUMBS_VIEW_WIDTH = existingConfiguration?.THUMBS_VIEW_WIDTH ?? 140,
             THUMBS_VIEW_HEIGHT = existingConfiguration?.THUMBS_VIEW_HEIGHT ?? 120,
             PREVIEW_THUMB_WIDTH = existingConfiguration?.PREVIEW_THUMB_WIDTH ?? 300,
             PREVIEW_THUMB_HEIGHT = existingConfiguration?.PREVIEW_THUMB_HEIGHT ?? 200,
             MAX_IMAGE_WIDTH = existingConfiguration?.MAX_IMAGE_WIDTH ?? _mediaSettings.MaximumImageSize,
             MAX_IMAGE_HEIGHT = existingConfiguration?.MAX_IMAGE_HEIGHT ?? _mediaSettings.MaximumImageSize,
             DEFAULTVIEW = existingConfiguration?.DEFAULTVIEW ?? "list",
             FORBIDDEN_UPLOADS = existingConfiguration?.FORBIDDEN_UPLOADS ?? string.Join(" ", NopRoxyFilemanDefaults.ForbiddenUploadExtensions),
             ALLOWED_UPLOADS = existingConfiguration?.ALLOWED_UPLOADS ?? string.Empty,
             FILEPERMISSIONS = existingConfiguration?.FILEPERMISSIONS ?? "0644",
             DIRPERMISSIONS = existingConfiguration?.DIRPERMISSIONS ?? "0755",
             LANG = existingConfiguration?.LANG ?? lang,
             DATEFORMAT = existingConfiguration?.DATEFORMAT ?? "dd/MM/yyyy HH:mm",
             OPEN_LAST_DIR = existingConfiguration?.OPEN_LAST_DIR ?? "yes",

             //no need user to configure
             INTEGRATION = "custom",
             RETURN_URL_PREFIX = $"{pathBase}/images/uploaded/",
             DIRLIST = $"{pathBase}/Admin/RoxyFileman/DirectoriesList",
             CREATEDIR = $"{pathBase}/Admin/RoxyFileman/CreateDirectory",
             DELETEDIR = $"{pathBase}/Admin/RoxyFileman/DeleteDirectory",
             MOVEDIR = $"{pathBase}/Admin/RoxyFileman/MoveDirectory",
             COPYDIR = $"{pathBase}/Admin/RoxyFileman/CopyDirectory",
             RENAMEDIR = $"{pathBase}/Admin/RoxyFileman/RenameDirectory",
             FILESLIST = $"{pathBase}/Admin/RoxyFileman/FilesList",
             UPLOAD = $"{pathBase}/Admin/RoxyFileman/UploadFiles",
             DOWNLOAD = $"{pathBase}/Admin/RoxyFileman/DownloadFile",
             DOWNLOADDIR = $"{pathBase}/Admin/RoxyFileman/DownloadDirectory",
             DELETEFILE = $"{pathBase}/Admin/RoxyFileman/DeleteFile",
             MOVEFILE = $"{pathBase}/Admin/RoxyFileman/MoveFile",
             COPYFILE = $"{pathBase}/Admin/RoxyFileman/CopyFile",
             RENAMEFILE = $"{pathBase}/Admin/RoxyFileman/RenameFile",
             GENERATETHUMB = $"{pathBase}/Admin/RoxyFileman/CreateImageThumbnail"
         };

         //save the file
         var text = JsonConvert.SerializeObject(configuration, Formatting.Indented);
         await File.WriteAllTextAsync(filePath, text, Encoding.UTF8);

         Singleton<RoxyFilemanConfig>.Instance = configuration;

         return configuration;
     }

     /// <summary>
     /// Get all available directories as a directory tree from database
     /// </summary>
     /// <param name="type">Type of the file</param>
     /// <param name="isRecursive">A value indicating whether to return a directory tree recursively</param>
     /// <param name="rootDirectoryPath">Path to start directory</param>
     public virtual IEnumerable<RoxyDirectoryInfo> GetDirectories(string type, bool isRecursive = true, string rootDirectoryPath = "")
     {
         // For database implementation, we simulate directories based on VirtualPath
         // Only get pictures that have a VirtualPath (uploaded through RoxyFileman)
         var pictures = _pictureService.GetPicturesAsync(rootDirectoryPath).Result
             .Where(p => !string.IsNullOrEmpty(p.VirtualPath))
             .ToList();
         
         var directories = new HashSet<string>();
         
         foreach (var picture in pictures)
         {
             var dirPath = GetDirectoryPath(picture.VirtualPath);
             if (!string.IsNullOrEmpty(dirPath) && dirPath != rootDirectoryPath)
             {
                 directories.Add(dirPath);
             }
         }

         foreach (var directory in directories)
         {
             var filesInDir = pictures.Count(p => GetDirectoryPath(p.VirtualPath ?? "") == directory);
             yield return new RoxyDirectoryInfo(directory, filesInDir, 0);
         }
     }

     /// <summary>
     /// Get files in the passed directory from database
     /// </summary>
     /// <param name="directoryPath">Path to the files directory</param>
     /// <param name="type">Type of the files</param>
     /// <returns>
     /// The list of <see cref="RoxyFileInfo"/>
     /// </returns>
     public virtual IEnumerable<RoxyFileInfo> GetFiles(string directoryPath = "", string type = "")
     {
         var pictures = _pictureService.GetPicturesAsync(directoryPath).Result;

         return pictures
             .Where(p => !string.IsNullOrEmpty(p.VirtualPath) && IsMatchType(p, type) && IsInDirectory(p, directoryPath))
             .Select(p =>
             {
                 var width = 0;
                 var height = 0;

                 if (GetFileType(p.SeoFilename + GetExtensionFromMimeType(p.MimeType)) == "image")
                 {
                     // For database images, we could load binary data to get dimensions
                     // For now, setting default values
                     width = 0;
                     height = 0;
                 }

                 var fileName = p.SeoFilename + GetExtensionFromMimeType(p.MimeType);
                 var fullPath = string.IsNullOrEmpty(directoryPath) ? fileName : Path.Combine(directoryPath, fileName);

                 return new RoxyFileInfo(fullPath, DateTimeOffset.Now, 0, width, height);
             });
     }

     /// <summary>
     /// Check if picture matches the requested type
     /// </summary>
     /// <param name="picture">Picture to check</param>
     /// <param name="type">Requested type</param>
     /// <returns>True if matches</returns>
     protected virtual bool IsMatchType(Picture picture, string type)
     {
         if (string.IsNullOrEmpty(type))
             return true;

         var fileName = picture.SeoFilename + GetExtensionFromMimeType(picture.MimeType);
         return GetFileType(fileName) == type;
     }

     /// <summary>
     /// Check if picture is in the specified directory (using normalized paths)
     /// </summary>
     /// <param name="picture">Picture to check</param>
     /// <param name="directoryPath">Directory path (already normalized)</param>
     /// <returns>True if in directory</returns>
     protected virtual bool IsInDirectoryNormalized(Picture picture, string directoryPath)
     {
         if (picture == null)
             return false;
             
         // Only consider pictures with VirtualPath (uploaded through RoxyFileman)
         if (string.IsNullOrEmpty(picture.VirtualPath))
             return false;
             
         if (string.IsNullOrEmpty(directoryPath))
         {
             // For root directory, check if picture is in root
             return !VirtualPathToDatabasePath(picture.VirtualPath).Contains('/');
         }
             
         var normalizedPicturePath = VirtualPathToDatabasePath(picture.VirtualPath);
         var pictureDirectory = GetDirectoryPath(normalizedPicturePath);
         
         return string.Equals(pictureDirectory, directoryPath, StringComparison.OrdinalIgnoreCase);
     }

     /// <summary>
     /// Check if picture is in the specified directory
     /// </summary>
     /// <param name="picture">Picture to check</param>
     /// <param name="directoryPath">Directory path</param>
     /// <returns>True if in directory</returns>
     protected virtual bool IsInDirectory(Picture picture, string directoryPath)
     {
         // Only consider pictures with VirtualPath (uploaded through RoxyFileman)
         if (string.IsNullOrEmpty(picture.VirtualPath))
             return false;

         // If directoryPath is empty, we're looking for root level files
         if (string.IsNullOrEmpty(directoryPath))
         {
             return !picture.VirtualPath.Contains('/') ||
                    GetDirectoryPath(picture.VirtualPath) == "";
         }

         var pictureDirPath = GetDirectoryPath(picture.VirtualPath);
         return string.Equals(pictureDirPath, directoryPath, StringComparison.OrdinalIgnoreCase);
     }

     /// <summary>
     /// Get file extension from MIME type
     /// </summary>
     /// <param name="mimeType">MIME type</param>
     /// <returns>File extension</returns>
     protected virtual string GetExtensionFromMimeType(string mimeType)
     {
         return mimeType?.ToLowerInvariant() switch
         {
             "image/jpeg" => ".jpg",
             "image/png" => ".png",
             "image/gif" => ".gif",
             "image/webp" => ".webp",
             "image/svg+xml" => ".svg",
             _ => ".jpg"
         };
     }

     /// <summary>
     /// Moves a specified file to a new location, providing the option to specify a new file name in database
     /// </summary>
     /// <param name="sourcePath">The name of the file to move. Can include a relative or absolute path</param>
     /// <param name="destinationPath">The new path and name for the file</param>
     public virtual void FileMove(string sourcePath, string destinationPath)
     {
         var sourceFile = FindPictureByPath(sourcePath);

         if (sourceFile == null)
             throw new RoxyFilemanException("E_MoveFileInvalisPath");

         var destinationFile = FindPictureByPath(destinationPath);
         if (destinationFile != null)
             throw new RoxyFilemanException("E_MoveFileAlreadyExists");

         try
         {
             var newVirtualPath = VirtualPathToDatabasePath(destinationPath);
             sourceFile.VirtualPath = newVirtualPath;
             sourceFile.SeoFilename = Path.GetFileNameWithoutExtension(destinationPath);
             _pictureService.UpdatePictureAsync(sourceFile).Wait();
         }
         catch
         {
             throw new RoxyFilemanException("E_MoveFile");
         }
     }

     /// <summary>
     /// Find a picture by its virtual path
     /// </summary>
     /// <param name="virtualPath">Virtual path</param>
     /// <returns>Picture if found</returns>
     protected virtual Picture FindPictureByPath(string virtualPath)
     {
         if (string.IsNullOrEmpty(virtualPath))
             return null;

         // Normalize the search path first
         var normalizedPath = VirtualPathToDatabasePath(virtualPath);
         var fileName = Path.GetFileNameWithoutExtension(normalizedPath);
         var directoryPath = GetDirectoryPath(normalizedPath);
         
         // Get all pictures and search by normalized paths
         var allPictures = _pictureService.GetPicturesAsync("").Result;
         
         // First try exact VirtualPath match (normalized)
         var picture = allPictures.FirstOrDefault(p => 
             !string.IsNullOrEmpty(p.VirtualPath) &&
             string.Equals(VirtualPathToDatabasePath(p.VirtualPath), normalizedPath, StringComparison.OrdinalIgnoreCase));

         // If not found, try by filename in the specific directory
         if (picture == null)
         {
             picture = allPictures.FirstOrDefault(p => 
                 string.Equals(p.SeoFilename, fileName, StringComparison.OrdinalIgnoreCase) &&
                 IsInDirectoryNormalized(p, directoryPath));
         }

         // If still not found, try by filename only (for root files)
         if (picture == null)
         {
             picture = allPictures.FirstOrDefault(p => 
                 string.Equals(p.SeoFilename, fileName, StringComparison.OrdinalIgnoreCase));
         }

         return picture;
     }

     /// <summary>
     /// Copy the directory with the embedded files and directories in database
     /// </summary>
     /// <param name="sourcePath">Path to the source directory</param>
     /// <param name="destinationPath">Path to the destination directory</param>
     public virtual void CopyDirectory(string sourcePath, string destinationPath)
     {
         var sourceDbPath = VirtualPathToDatabasePath(sourcePath);
         var destDbPath = VirtualPathToDatabasePath(destinationPath);

         var pictures = _pictureService.GetPicturesAsync(sourceDbPath).Result;
         var picturesToCopy = pictures.Where(p => IsInDirectory(p, sourceDbPath)).ToList();

         if (!picturesToCopy.Any())
             throw new RoxyFilemanException("E_CopyDirInvalidPath");

         try
         {
             foreach (var picture in picturesToCopy)
             {
                 // Load the binary data
                 var binaryData = _pictureService.LoadPictureBinaryAsync(picture).Result;
                 
                 // Create new picture in destination directory
                 var newVirtualPath = picture.VirtualPath?.Replace(sourceDbPath, destDbPath);
                 var newPicture = _pictureService.InsertPictureAsync(
                     binaryData,
                     picture.MimeType,
                     picture.SeoFilename,
                     picture.AltAttribute,
                     picture.TitleAttribute).Result;
                 
                 newPicture.VirtualPath = newVirtualPath;
                 _pictureService.UpdatePictureAsync(newPicture).Wait();
             }
         }
         catch
         {
             throw new RoxyFilemanException("E_CopyFile");
         }
     }

     /// <summary>
     /// Rename the directory
     /// </summary>
     /// <param name="sourcePath">Path to the source directory</param>
     /// <param name="newName">New name of the directory</param>
     /// <returns>A task that represents the asynchronous operation</returns>
     public virtual void RenameDirectory(string sourcePath, string newName)
     {
         try
         {
             var destinationPath = Path.Combine(Path.GetDirectoryName(sourcePath), newName);
             DirectoryMove(sourcePath, destinationPath);
         }
         catch (Exception ex)
         {
             throw new RoxyFilemanException("E_RenameDir", ex);
         }
     }

     /// <summary>
     /// Rename the file
     /// </summary>
     /// <param name="sourcePath">Path to the source file</param>
     /// <param name="newName">New name of the file</param>
     /// <returns>A task that represents the asynchronous operation</returns>
     public virtual void RenameFile(string sourcePath, string newName)
     {
         try
         {
             var destinationPath = Path.Combine(Path.GetDirectoryName(sourcePath), newName);
             FileMove(sourcePath, destinationPath);
         }
         catch (Exception ex)
         {
             throw new RoxyFilemanException("E_RenameFile", ex);
         }
     }

     /// <summary>
     /// Delete the file from database
     /// </summary>
     /// <param name="path">Path to the file</param>
     /// <returns>A task that represents the asynchronous operation</returns>
     public virtual void DeleteFile(string path)
     {
         var fileToDelete = FindPictureByPath(path);

         if (fileToDelete == null)
             throw new RoxyFilemanException("E_DeleteFileInvalidPath");

         try
         {
             _pictureService.DeletePictureAsync(fileToDelete).Wait();
         }
         catch
         {
             throw new RoxyFilemanException("E_DeleteFile");
         }
     }

     /// <summary>
     /// Copy the file in database
     /// </summary>
     /// <param name="sourcePath">Path to the source file</param>
     /// <param name="destinationPath">Path to the destination file</param>
     /// <returns>A task that represents the asynchronous operation</returns>
     public virtual void CopyFile(string sourcePath, string destinationPath)
     {
         var sourceFile = FindPictureByPath(sourcePath);

         if (sourceFile == null)
             throw new RoxyFilemanException("E_CopyFileInvalidPath");

         var fileName = Path.GetFileNameWithoutExtension(sourcePath);
         var destinationFile = FindPictureByPath(Path.Combine(destinationPath, fileName + GetExtensionFromMimeType(sourceFile.MimeType)));

         var newFileName = fileName;
         if (destinationFile != null)
             newFileName = GetUniqueFileName(destinationPath, fileName + GetExtensionFromMimeType(sourceFile.MimeType));

         try
         {
             // Load the binary data
             var binaryData = _pictureService.LoadPictureBinaryAsync(sourceFile).Result;
             
             // Create new picture in destination directory
             var newPicture = _pictureService.InsertPictureAsync(
                 binaryData,
                 sourceFile.MimeType,
                 Path.GetFileNameWithoutExtension(newFileName),
                 sourceFile.AltAttribute,
                 sourceFile.TitleAttribute).Result;
             
             newPicture.VirtualPath = VirtualPathToDatabasePath(Path.Combine(destinationPath, newFileName));
             _pictureService.UpdatePictureAsync(newPicture).Wait();
         }
         catch
         {
             throw new RoxyFilemanException("E_CopyFile");
         }
     }

     /// <summary>
     /// Create the new directory in database (conceptual - directories are virtual in database)
     /// </summary>
     /// <param name="parentDirectoryPath">Path to the parent directory</param>
     /// <param name="name">Name of the new directory</param>
     /// <returns>A task that represents the asynchronous operation</returns>
     public virtual void CreateDirectory(string parentDirectoryPath, string name)
     {
         //In database implementation, directories are created virtually when files are added
         //This method serves as a placeholder for the interface requirement
         var virtualPath = VirtualPathToDatabasePath(Path.Combine(parentDirectoryPath, name));
         
         if (!IsValidPath(virtualPath))
             throw new RoxyFilemanException("E_CreateDir");
         
         // Directory creation is implicit in database - no action needed
     }

     /// <summary>
     /// Delete the directory from database
     /// </summary>
     /// <param name="path">Path to the directory</param>
     public virtual void DeleteDirectory(string path)
     {
         var sourcePath = VirtualPathToDatabasePath(path);
         var pictures = _pictureService.GetPicturesAsync(sourcePath).Result;
         var picturesToDelete = pictures.Where(p => IsInDirectory(p, sourcePath)).ToList();

         if (!picturesToDelete.Any())
             throw new RoxyFilemanException("E_DeleteDirInvalidPath");

         // Check if this is the root directory (shouldn't be deleted)
         if (string.IsNullOrEmpty(sourcePath))
             throw new RoxyFilemanException("E_CannotDeleteRoot");

         if (picturesToDelete.Any())
             throw new RoxyFilemanException("E_DeleteNonEmpty");

         // In database implementation, directories are virtual and deleted automatically
         // when all files are removed, so no action is needed here
     }

     /// <summary>
     /// Save file in the database
     /// </summary>
     /// <param name="directoryPath">Directory path in the database</param>
     /// <param name="fileName">The file name and extension</param>
     /// <param name="contentType">Mime type</param>
     /// <param name="fileStream">The stream to read</param>
     /// <returns>A task that represents the asynchronous operation</returns>
     public virtual async Task SaveFileAsync(string directoryPath, string fileName, string contentType, Stream fileStream)
     {
         var uniqueFileName = GetUniqueFileName(directoryPath, Path.GetFileName(fileName));
         var seoFileName = Path.GetFileNameWithoutExtension(uniqueFileName);

         using var memoryStream = new MemoryStream();
         await fileStream.CopyToAsync(memoryStream);
         var fileData = memoryStream.ToArray();

         if (GetFileType(Path.GetExtension(uniqueFileName)) == "image")
         {
             var roxyConfig = Singleton<RoxyFilemanConfig>.Instance;

             fileData = ResizeImage(fileData,
                 GetImageFormatByMimeType(contentType),
                 roxyConfig?.MAX_IMAGE_WIDTH ?? _mediaSettings.MaximumImageSize,
                 roxyConfig?.MAX_IMAGE_HEIGHT ?? _mediaSettings.MaximumImageSize);
         }

         // Create the picture in database
         var picture = await _pictureService.InsertPictureAsync(
             fileData,
             contentType,
             seoFileName);

         // Set the virtual path - normalize the directory path consistently
         var normalizedDirectoryPath = VirtualPathToDatabasePath(directoryPath ?? "");
         var virtualPath = string.IsNullOrEmpty(normalizedDirectoryPath) 
             ? uniqueFileName 
             : $"{normalizedDirectoryPath}/{uniqueFileName}";
         
         // Store the normalized path for consistent lookups
         picture.VirtualPath = virtualPath;
         await _pictureService.UpdatePictureAsync(picture);
     }

     /// <summary>
     /// Get the thumbnail of the image
     /// </summary>
     /// <param name="sourcePath">File path</param>
     /// <param name="contentType">Mime type</param>
     /// <returns>Byte array of the specified image</returns>
     public virtual byte[] CreateImageThumbnail(string sourcePath, string contentType)
     {
         var picture = FindPictureByPath(sourcePath);

         if (picture == null)
             throw new RoxyFilemanException("Image not found");

         var roxyConfig = Singleton<RoxyFilemanConfig>.Instance;

         // Load binary data from database
         var binaryData = _pictureService.LoadPictureBinaryAsync(picture).Result;

         if (binaryData == null || binaryData.Length == 0)
             throw new RoxyFilemanException("Image data not found");

         return ResizeImage(
             binaryData,
             GetImageFormatByMimeType(contentType),
             roxyConfig.THUMBS_VIEW_WIDTH,
             roxyConfig.THUMBS_VIEW_HEIGHT);
     }

     /// <summary>
     /// Create a zip archive of the specified directory from database.
     /// </summary>
     /// <param name="directoryPath">The directory path with files to compress</param>
     /// <returns>The byte array</returns>
     public virtual byte[] CreateZipArchiveFromDirectory(string directoryPath)
     {
         var sourcePath = VirtualPathToDatabasePath(directoryPath);
         var pictures = _pictureService.GetPicturesAsync(sourcePath).Result;
         var picturesToZip = pictures.Where(p => IsInDirectory(p, sourcePath)).ToList();

         if (!picturesToZip.Any())
             throw new RoxyFilemanException("E_CreateArchive");

         using var memoryStream = new MemoryStream();
         using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
         {
             foreach (var picture in picturesToZip)
             {
                 var fileName = picture.SeoFilename + GetExtensionFromMimeType(picture.MimeType);
                 var fileRelativePath = string.IsNullOrEmpty(picture.VirtualPath) ? fileName : picture.VirtualPath;
                 
                 var binaryData = _pictureService.LoadPictureBinaryAsync(picture).Result;
                 
                 using var fileStream = new MemoryStream(binaryData);
                 using var fileStreamInZip = archive.CreateEntry(fileRelativePath).Open();
                 fileStream.CopyTo(fileStreamInZip);
             }
         }

         //ToArray() should be outside of the archive using
         return memoryStream.ToArray();
     }

     #endregion
 }