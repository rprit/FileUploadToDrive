using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Diagnostics;

namespace FileUploadToDrive
{
    internal class Program
    {
        internal static string jsonPath;
        internal static string tokenStoragePath;

        [STAThread]
        internal static void Main(string[] args)
        {
            Console.WriteLine("Hello, welcome to Upload a File!");

            Console.WriteLine("Select the json file having your client secrets...");
            jsonPath = SelectFiles(false).FirstOrDefault();

            Console.WriteLine("Select the folder for your token storage...");
            tokenStoragePath=SelectFolder(); 

            //This method uploads the given file to google drive
            UploadSingleFile();

            Console.WriteLine();

            //This method allows user to select multiple files and uploads them to google drive first in sequential
            //and then in parallel way
            UploadMultipleFiles();

            Console.WriteLine("\nPress a key to exit...");
            Console.ReadKey();            
        }

        internal static async void UploadMultipleFiles()
        {
            Console.WriteLine("Select the files to be uploaded...");

            List<string> files = SelectFiles(true);
            
            var tokenStorage = new FileDataStore(tokenStoragePath, true);

            UserCredential credential;

            await using (var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { DriveService.ScopeConstants.Drive },
                    "user",
                    CancellationToken.None,
                    tokenStorage).Result;
            }

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            });

            var stopWatch = Stopwatch.StartNew();

            //Sequential execution
            foreach (var file in files)
            {
                FilesResource.CreateMediaUpload request;

                // Create a new file on drive.
                using (var fstream = new FileStream(file, FileMode.Open))
                {
                    // Create a new file, with metadata and stream.
                    request = service.Files.Create(new Google.Apis.Drive.v3.Data.File() {
                                    Name = Path.GetFileName(file),
                                    MimeType = GetMimeType(file)
                                }, fstream, GetMimeType(file));
                    request.Fields = "id";
                    request.Upload();
                }
                Console.WriteLine($"File Name: {file}, Thread Id: {Thread.CurrentThread.ManagedThreadId}");
            }
            Console.WriteLine("Sequential execution time = {0} seconds\n", stopWatch.Elapsed.TotalSeconds);

            stopWatch = Stopwatch.StartNew();

            //Parallel execution
            Parallel.ForEach(files, file =>
            {
                Google.Apis.Drive.v3.Data.File fileMetadata = new()
                {
                    Name = Path.GetFileName(file),
                    MimeType = GetMimeType(file)
                };

                FilesResource.CreateMediaUpload request;

                // Create a new file on drive.
                using (var fstream = new FileStream(file, FileMode.Open))
                {
                    // Create a new file, with metadata and stream.
                    request = service.Files.Create(fileMetadata, fstream, GetMimeType(file));
                    request.Fields = "id";
                    request.Upload();
                }
                Console.WriteLine($"File Name: {file}, Thread Id: {Thread.CurrentThread.ManagedThreadId}");
            });
            Console.WriteLine("Multi threaded execution time = {0} seconds", stopWatch.Elapsed.TotalSeconds);
        }

        private static string GetMimeType(string fileName)
        {
            string mimeType = "application/unknown";
            string ext = Path.GetExtension(fileName).ToLower();
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey != null && regKey.GetValue("Content Type") != null)
                mimeType = regKey.GetValue("Content Type").ToString();
            return mimeType;
        }

        private static List<string> SelectFiles(bool multiSelect)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = multiSelect;
            List<string> files = new();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in dialog.FileNames)
                {
                    files.Add(file);
                }
            }
            return files;
        }

        private static string SelectFolder()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            string path="";
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                path = fbd.SelectedPath;
            }
            return path;
        }

        
        internal static async void UploadSingleFile()
        {
            Console.WriteLine("Select the file to be uploaded...");

            var path = SelectFiles(false).FirstOrDefault();

            var tokenStorage = new FileDataStore(tokenStoragePath, true);

            UserCredential credential;

            await using (var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { DriveService.ScopeConstants.Drive },
                    "user",
                    CancellationToken.None,
                    tokenStorage).Result;
            }

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            });

            FilesResource.CreateMediaUpload request;

            // Create a new file on drive.
            using (var stream = new FileStream(path, FileMode.Open))
            {
                // Create a new file, with metadata and stream.
                request = service.Files.Create(new Google.Apis.Drive.v3.Data.File(){
                                    Name = "My File To Upload.txt"
                                }, stream, "text/plain");
                request.Fields = "id";
                request.Upload();
            }

            var file = request.ResponseBody;

            // Prints the uploaded file id.
            Console.WriteLine("File has been uploaded. File ID: " + file.Id);

        }
    }
}
