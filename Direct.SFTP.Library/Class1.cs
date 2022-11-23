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

        private static bool sendFile(ConnectionInfo connectionInfo, string destFolder, string sourceFolder, string file)
        {
            using (var sftp = new SftpClient(connectionInfo))
            {
                try
                {
                    sftp.Connect();

                    if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Connected: " + sftp.IsConnected.ToString()); }

                    if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Setting source directory: " + destFolder); }
                    sftp.ChangeDirectory(destFolder);

                    if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Reading file: " + sourceFolder + file); }
                    using (var uplfileStream = File.OpenRead(sourceFolder + file))
                    {
                        if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - File read, size: " + uplfileStream.Length); }
                        if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Uploading file:" + file); }
                        sftp.UploadFile(uplfileStream, file, true);
                    }

                    destFolder = destFolder.EndsWith(@"/") ? destFolder : destFolder + @"/";
                    destFolder = destFolder.StartsWith(@"/") ? destFolder : @"/" + destFolder;
                    if (!sftp.Exists(destFolder + file))
                    {
                        logArchitect.Debug("Direct.SFTP.Library - File: " + destFolder + file);
                        logArchitect.Error("Direct.SFTP.Library - File validation failed");
                        sftp.Disconnect();
                        if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Disconnected"); }
                        return false;
                    }

                    sftp.Disconnect();
                    if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Disconnected"); }

                    return true;
                }
                catch (Exception e)
                {
                    if (sftp.IsConnected)
                    {
                        if (logArchitect.IsDebugEnabled) { logArchitect.Debug("Direct.SFTP.Library - Disconnecting on error"); }
                        sftp.Disconnect();
                    }
                    throw e;
                }
                
            }

        }

        [DirectDom("Send file via SFTP with SSH PrivateKey")]
        [DirectDomMethod("Send {folder} {file} to {directory} on {host}  with {credentials} {privateKeyFullPath} {passPhrasePrivateKey}")]
        [MethodDescription("Send a file from a folder (e.g. D:/MyFolder/) via SFTP to the destination host and directory (e.g. /MyFolder/) and validate if it was written. Uses private key in pem fomat and credentials as authentication.")]
        public static bool SendFileSFTPWithKey(string sourcefolder, string file, string destfolder, string host, AppLoginInfo creds, string privateKey, AppLoginInfo keyPassPhrase)
        {
            try
            {
                PrivateKeyFile privateKeyFile = new PrivateKeyFile(privateKey, keyPassPhrase.Password);
                PrivateKeyFile[] privateKeyFiles = new PrivateKeyFile[] { privateKeyFile };
                List<AuthenticationMethod> authenticationMethods = new List<AuthenticationMethod>()
                {
                    new PasswordAuthenticationMethod(creds.UserName, creds.Password),
                    new PrivateKeyAuthenticationMethod(creds.UserName, privateKeyFiles)
                };
                ConnectionInfo connectionInfo;

                if (host.Contains(":"))
                {
                    string server = host.Split(':')[0];
                    int port = 0;
                    int.TryParse(host.Split(':')[1], out port);
                    connectionInfo = new ConnectionInfo(server, port, "sftp", authenticationMethods.ToArray());
                    logArchitect.Info("Direct.SFTP.Library - Connecting to: " + server + " and port: " + port.ToString());
                }
                else
                {
                    connectionInfo = new ConnectionInfo(host, "sftp", authenticationMethods.ToArray());
                    logArchitect.Info("Direct.SFTP.Library - Connecting to: " + host);
                }

                return sendFile(connectionInfo, destfolder, sourcefolder, file);

            }
            catch (Exception e)
            {
                logArchitect.Error("Direct.SFTP.Library - Send File Exception", e);
                return false;
            }
        }

        [DirectDom("Send file via SFTP with private key auth only")]
        [DirectDomMethod("Send {folder} {file} to {directory} on {host} with {private key} and {key info}")]
        [MethodDescription("Send a file from a folder (e.g. D:/MyFolder/) via SFTP to the destination host and directory (e.g. /MyFolder/) and validate if it was written. Uses private key as authentication method. Key has to be in PEM format. Key info has to contain username and private key passphrase as password")]
        public static bool SendFileSFTPWithPrivateKeyAuth(string sourcefolder, string file, string destfolder, string host, string privateKeyPath, AppLoginInfo creds) 
        {
            try
            {
                string privateKeyFileExtension = Path.GetExtension(privateKeyPath);

                if (!privateKeyFileExtension.Contains("pem"))
                {
                    throw new Exception("Provided private key file is not of pem extension.");
                }

                ConnectionInfo connectionInfo;
                logArchitect.Info("Direct.SFTP.Library - creating private key file instance from file: " + privateKeyPath);
                PrivateKeyFile privateKey = new PrivateKeyFile(privateKeyPath, creds.Password);
                logArchitect.Info("Direct.SFTP.Library - private key successfully added");
                logArchitect.Info("Direct.SFTP.Library - adding key to private keys array");
                PrivateKeyFile[] privateKeyFiles = new PrivateKeyFile[] { privateKey };
                logArchitect.Info("Direct.SFTP.Library - file successully added to the array, length: " + privateKeyFiles.Length);

                if (host.Contains(":"))
                {
                    string server = host.Split(':')[0];
                    int port = 0;
                    int.TryParse(host.Split(':')[1], out port);
                    logArchitect.Info("Direct.SFTP.Library - Creating connection info to: " + server + " and port: " + port.ToString());
                    connectionInfo = new ConnectionInfo(server, port, creds.UserName, new PrivateKeyAuthenticationMethod(creds.UserName, privateKeyFiles));
                    logArchitect.Info("Direct.SFTP.Library - Connecting to: " + server + " and port: " + port.ToString());
                }
                else
                {
                    logArchitect.Info("Direct.SFTP.Library - Creating connection info to: " + host);
                    connectionInfo = new ConnectionInfo(host, creds.UserName, new PrivateKeyAuthenticationMethod(creds.UserName, privateKeyFiles));
                    logArchitect.Info("Direct.SFTP.Library - Connecting to: " + host);
                }

                return sendFile(connectionInfo, destfolder, sourcefolder, file);

            }
            catch (Exception e)
            {
                logArchitect.Error("Direct.SFTP.Library - Send File Exception", e);
                return false;
            }
        }


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

                return sendFile(connectionInfo, destfolder, sourcefolder, file);
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



