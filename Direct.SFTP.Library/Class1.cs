using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using log4net;
using Direct.Shared;
using System.Net;
using System.IO;

namespace Direct.SFTP.Library
{
    [DirectSealed]
    [DirectDom("SFTP", "FileTransfer", false)]
    [ParameterType(false)]
    public class SFTP
    {
        private static readonly ILog logArchitect = LogManager.GetLogger(Loggers.LibraryObjects);

        [DirectDom("Send file via SFTP")]
        [DirectDomMethod("Send {folder} {file} to {directory} on {host}  with {credentials}")]
        [MethodDescription("Send a file from a folder (e.g. D:/MyFolder/) via SFTP to the destination host and directory (e.g. /MyFolder/) and validate if it was written")]
        public static bool SendFileSFTP(string sourcefolder, string file, string destfolder, string host, AppLoginInfo creds)
        {
            try
            {
                ConnectionInfo connectionInfo;

                if (host.Contains(":"))
                {
                    string server = host.Split(':')[0];
                    int port = 0;
                    Int32.TryParse(host.Split(':')[1], out port);
                    connectionInfo = new ConnectionInfo(server, port, "sftp", new PasswordAuthenticationMethod(creds.UserName, creds.Password));
                    logArchitect.Info("Direct.SFTP.Library - Connecting to: " + server + " and port: " + port.ToString());
                }
                else
                {
                    connectionInfo = new ConnectionInfo(host, "sftp", new PasswordAuthenticationMethod(creds.UserName, creds.Password));
                    logArchitect.Info("Direct.SFTP.Library - Connecting to: " + host);
                }

                using (var sftp = new SftpClient(connectionInfo))
                {
                    sftp.Connect();

                    if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Setting source directory: " + destfolder); }
                    sftp.ChangeDirectory(destfolder);

                    using (var uplfileStream = System.IO.File.OpenRead(sourcefolder + file))
                    {
                        if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Uploading file:" + file); }
                        sftp.UploadFile(uplfileStream, file, true);
                    }

                    destfolder = destfolder.EndsWith(@"/") ? destfolder : destfolder + @"/";
                    destfolder = destfolder.StartsWith(@"/") ? destfolder : @"/" + destfolder;
                    if (!sftp.Exists(destfolder + file))
                    {
                        logArchitect.Debug("Direct.SFTP.Library - File: " + destfolder + file);
                        logArchitect.Error("Direct.SFTP.Library - File validation failed");
                        sftp.Disconnect();
                        if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Disconnected"); }
                        return false;
                    }

                    sftp.Disconnect();
                    if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Disconnected"); }
                }
                return true;
            }
            catch (Exception e)
            {
                logArchitect.Error("Direct.SFTP.Library - Send File Exception", e);
                return false;
            }
        } 

        [DirectDom("Check if file exists via SFTP")]
        [DirectDomMethod("Check if {file} exists in {directory} on {host} with {credentials}")]
        [MethodDescription("Validate if a file exists via SFTP")]
        public static bool ValidateIfFileExistsSFTP(string file, string destfolder, string host, AppLoginInfo creds)
        {
            try
            {
                ConnectionInfo connectionInfo;

                if (host.Contains(":"))
                {
                    string server = host.Split(':')[0];
                    int port = 0;
                    Int32.TryParse(host.Split(':')[1], out port);
                    connectionInfo = new ConnectionInfo(server, port, "sftp", new PasswordAuthenticationMethod(creds.UserName, creds.Password));
                    logArchitect.Info("Direct.SFTP.Library - Connecting to: " + server + " and port: " + port.ToString());
                }
                else
                {
                    connectionInfo = new ConnectionInfo(host, "sftp", new PasswordAuthenticationMethod(creds.UserName, creds.Password));
                    logArchitect.Info("Direct.SFTP.Library - Connecting to: " + host);
                }

                using (var sftp = new SftpClient(connectionInfo))
                {

                    sftp.Connect();
                    logArchitect.Info("Direct.SFTP.Library - Connected to: " + host);

                    destfolder = destfolder.EndsWith(@"/") ? destfolder : destfolder + @"/";
                    destfolder = destfolder.StartsWith(@"/") ? destfolder : @"/" + destfolder;
                    if (!sftp.Exists(destfolder + file))
                    {
                        logArchitect.Debug("Direct.SFTP.Library - File does not exist: " + destfolder + file);
                        sftp.Disconnect();
                        if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Disconnected"); }
                        return false;
                    }
                    else
                    {
                        logArchitect.Debug("Direct.SFTP.Library - File exists: " + destfolder + file);
                        sftp.Disconnect();
                        if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Disconnected"); }
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                logArchitect.Error("Direct.SFTP.Library - Validate File Exception", e);
                return false;
            }
        }

        [DirectDom("Delete file via SFTP")]
        [DirectDomMethod("Delete {file} in {directory} on {host} with {credentials}")]
        [MethodDescription("Delete a file via SFTP")]
        public static bool DeleteFileSFTP(string file, string destfolder, string host, AppLoginInfo creds)
        {
            try
            {
                ConnectionInfo connectionInfo;

                if (host.Contains(":"))
                {
                    string server = host.Split(':')[0];
                    int port = 0;
                    Int32.TryParse(host.Split(':')[1], out port);
                    connectionInfo = new ConnectionInfo(server, port, "sftp", new PasswordAuthenticationMethod(creds.UserName, creds.Password));
                    logArchitect.Info("Direct.SFTP.Library - Connecting to: " + server + " and port: " + port.ToString());
                }
                else
                {
                    connectionInfo = new ConnectionInfo(host, "sftp", new PasswordAuthenticationMethod(creds.UserName, creds.Password));
                    logArchitect.Info("Direct.SFTP.Library - Connecting to: " + host);
                }

                using (var sftp = new SftpClient(connectionInfo))
                {

                    sftp.Connect();
                    logArchitect.Info("Direct.SFTP.Library - Connected to: " + host);

                    if (!sftp.Exists(destfolder + file))
                    {
                        logArchitect.Debug("Direct.SFTP.Library - File does not exist: " + destfolder + file);
                        sftp.Disconnect();
                        if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Disconnected"); }
                        return false;
                    }
                    else
                    {
                        logArchitect.Debug("Direct.SFTP.Library - File exists: " + destfolder + file);
                        sftp.DeleteFile(destfolder + file);
                        logArchitect.Debug("Direct.SFTP.Library - File deleted: " + destfolder + file);
                        sftp.Disconnect();
                        if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Disconnected"); }
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                logArchitect.Error("Direct.SFTP.Library - Delete File Exception", e);
                return false;
            }
        }


        [DirectDom("Download file from SFTP")]
        [DirectDomMethod("Download {file} from {directory} on {host} to {filepath}  with {credentials}")]
        [MethodDescription("Download a file from a folder (e.g. /MyFolder/) via SFTP to the destination host and directory (e.g. D:/MyFolder/) and validate if it was written")]
        public static bool DownloadFileSFTP(string file, string sourcefolder, string host, string destfolder, AppLoginInfo creds)
        {

            try {
                ConnectionInfo connectionInfo;

                if (host.Contains(":"))
                {
                    string server = host.Split(':')[0];
                    int port = 0;
                    Int32.TryParse(host.Split(':')[1], out port);
                    connectionInfo = new ConnectionInfo(server, port, "sftp", new PasswordAuthenticationMethod(creds.UserName, creds.Password));
                    logArchitect.Info("Direct.SFTP.Library - Connecting to: " + server + " and port: " + port.ToString());
                }
                else
                {
                    connectionInfo = new ConnectionInfo(host, "sftp", new PasswordAuthenticationMethod(creds.UserName, creds.Password));
                    logArchitect.Info("Direct.SFTP.Library - Connecting to: " + host);
                }

                using (var sftp = new SftpClient(connectionInfo))
                {
                    sftp.Connect();
                    using (Stream file1 = File.OpenWrite(destfolder))
                    {
                        if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.FTP.Library - Download file:" + file + " from host: " + host + sourcefolder + " to: " + destfolder); }
                        sftp.DownloadFile(sourcefolder + file, file1);
                        return true;
                    }
                    

                }
            }
            catch (Exception e)
            {
                logArchitect.Error("Direct.SFTP.Library - Download File Exception", e);
                return false;
            }
        }

    }
}


    [DirectSealed]
    [DirectDom("FTP", "FileTransfer", false)]
    [ParameterType(false)]
    public class FTP 
    {
        private static readonly ILog logArchitect = LogManager.GetLogger(Loggers.LibraryObjects);
        [DirectDom("Send file via FTP")]
        [DirectDomMethod("Send {folder} {file} to {directory} on {host}  with {credentials}")]
        [MethodDescription("Send a file from a folder (e.g. D:/MyFolder/) via FTP to the destination host and directory (e.g. /MyFolder/) and validate if it was written")]
        public static bool SendFileFTP(string sourcefolder, string file, string destfolder, string host, AppLoginInfo creds)
        {
            try { 
                WebClient client = new WebClient();
                client.Credentials = new NetworkCredential(creds.UserName, creds.Password);
                if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.FTP.Library - Upload file:" + file + " from host: " + host + sourcefolder + " to: " + destfolder); }
                client.UploadFile(
                    "ftp://" + host + destfolder + file, sourcefolder + file);
                return true;
            }
            catch (Exception e)
            {
                logArchitect.Error("Direct.FTP.Library - Upload File Exception", e);
                return false;
            }
}

        [DirectDom("Download file from FTP")]
        [DirectDomMethod("Download {file} from {directory} on {host} to {filepath}  with {credentials}")]
        [MethodDescription("Send a file from a folder (e.g. D:/MyFolder/) via FTP to the destination host and directory (e.g. /MyFolder/) and validate if it was written")]
        public static bool DownloadFileFTP(string file, string sourcefolder, string host, string destfolder, AppLoginInfo creds)
        {
            try
            {
                WebClient client = new WebClient();
                client.Credentials = new NetworkCredential(creds.UserName, creds.Password);
                if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.FTP.Library - Download file:" + file + " from host: " + host + sourcefolder + " to: " + destfolder); }
                client.DownloadFile(
                    "ftp://" + host + sourcefolder + file, destfolder);
                return true;
            }
            catch (Exception e)
            {
                logArchitect.Error("Direct.FTP.Library - Download File Exception", e);
                return false;
            }
}


        [DirectDom("Check if file exists via FTP")]
        [DirectDomMethod("Check if {file} exists in {directory} on {host} with {credentials}")]
        [MethodDescription("Validate if a file exists via FTP")]
        public static bool ValidateIfFileExistsFTP(string file, string destfolder, string host, AppLoginInfo creds)
        {
            var request = (FtpWebRequest)WebRequest.Create
                ("ftp://" + host + destfolder + file);
            request.Credentials = new NetworkCredential(creds.UserName, creds.Password);
            request.Method = WebRequestMethods.Ftp.GetFileSize;

            try
            {
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.FTP.Library - file:" + file + " Exists"); }
                return true;
            }
            catch (WebException e)
            {
                FtpWebResponse response = (FtpWebResponse)e.Response;
                if (response.StatusCode ==
                    FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.FTP.Library - file:" + file + " Does not exists"); }
                    return false;
                }
                else
                {
                    logArchitect.Error("Direct.FTP.Library - File Exist Exception", e);
                    return false; // false negative
                }
            }
        }

        [DirectDom("Delete file via FTP")]
        [DirectDomMethod("Delete {file} in {directory} on {host} with {credentials}")]
        [MethodDescription("Delete a file via FTP")]
        public static bool DeleteFileFTP(string file, string destfolder, string host, AppLoginInfo creds)
        {
            try 
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + host + destfolder + file);

                request.Credentials = new NetworkCredential(creds.UserName, creds.Password);

                request.Method = WebRequestMethods.Ftp.DeleteFile;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Console.WriteLine("Delete status: {0}", response.StatusDescription);
                response.Close();
                if (response.StatusCode == FtpStatusCode.FileActionOK)
                {
                    if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.FTP.Library - file:" + file + " Deleted"); }
                    return true;

                }
                else
                {
                    if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.FTP.Library - file:" + file + " not deleted: " + response.StatusDescription); }
                    return false;
                }
            }
            catch (Exception e)
            {
                logArchitect.Error("Direct.FTP.Library - Delete File Exception", e);
                return false;
            }

        }
    }



