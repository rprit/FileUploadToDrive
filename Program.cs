using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Diagnostics;

namespace FileUploadToDrive
{
    internal class Program
    {
        internal static IList<string> files = new List<string>();
        internal const string jsonPath = "F:\\Prithvi\\Projects\\FileUploadToDriveFiles\\Credentials.json";
        internal const string tokenStoragePath = "F:\\Prithvi\\Projects\\FileUploadToDriveFilestokenStorage";
        
        internal static void Main(string[] args)
        {
            Console.WriteLine("Hello, Upload a file!");

            //This method uploads the given file to google drive
            SingleFile();

            //This method allows user to select multiple files and uploads them to google drive first in sequential
            //and then in parallel way
            MultipleFiles();

            Console.ReadKey();            
        }

        internal static async void MultipleFiles()
        {
            Thread thread = new Thread(SelectFiles);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

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

        private static void SelectFiles()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in dialog.FileNames)
                {
                    files.Add(file);
                }
            }
        }

        
        internal static async void SingleFile()
        {
            var path = "F:\\Prithvi\\Projects\\FileUploadToDriveFiles\\My File To Upload.txt";

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

            // Upload file photo.jpg on drive.
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = "My File To Upload.txt"
            };
            FilesResource.CreateMediaUpload request;

            // Create a new file on drive.
            using (var stream = new FileStream(path,
                       FileMode.Open))
            {
                // Create a new file, with metadata and stream.
                request = service.Files.Create(fileMetadata, stream, "text/plain");
                request.Fields = "id";
                request.Upload();
            }

            var file = request.ResponseBody;

            // Prints the uploaded file id.
            Console.WriteLine("File has been uploaded. File ID: " + file.Id);

        }
    }
}
