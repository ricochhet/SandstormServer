﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Network.Certificate;
using Titanium.Web.Proxy.Shared;

namespace Titanium.Web.Proxy.Network;

/// <summary>
///     Certificate Engine option.
/// </summary>
public enum CertificateEngine
{
    /// <summary>
    ///     Uses BouncyCastle 3rd party library.
    ///     Default.
    /// </summary>
    BouncyCastle = 0,

    BouncyCastleFast = 2,

    /// <summary>
    ///     Uses Windows Certification Generation API and only valid in Windows OS.
    ///     Observed to be faster than BouncyCastle.
    ///     Bug #468 Reported.
    /// </summary>
    DefaultWindows = 1
}

/// <summary>
///     A class to manage SSL certificates used by this proxy server.
/// </summary>
public sealed class CertificateManager : IDisposable
{
    private const string DefaultRootCertificateIssuer = "Titanium";

    private const string DefaultRootRootCertificateName = "Titanium Root Certificate Authority";

    private static readonly ConcurrentDictionary<string, object> _saveCertificateLocks = new();

    /// <summary>
    ///     Cache dictionary
    /// </summary>
    private readonly ConcurrentDictionary<string, CachedCertificate> cachedCertificates = new();

    private readonly CancellationTokenSource clearCertificatesTokenSource = new();

    /// <summary>
    ///     Used to prevent multiple threads working on same certificate generation
    ///     when burst certificate generation requests happen for same certificate.
    /// </summary>
    private readonly SemaphoreSlim pendingCertificateCreationTaskLock = new(1);

    /// <summary>
    ///     A list of pending certificate creation tasks.
    /// </summary>
    private readonly Dictionary<string, Task<X509Certificate2?>> pendingCertificateCreationTasks = new();

    private readonly object rootCertCreationLock = new();

    private ICertificateMaker? certEngineValue;

    private ICertificateCache certificateCache = new DefaultCertificateDiskCache();

    private bool disposed;

    private CertificateEngine engine;

    private string? issuer;

    private X509Certificate2? rootCertificate;

    private string? rootCertificateName;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CertificateManager" /> class.
    /// </summary>
    /// <param name="rootCertificateName"></param>
    /// <param name="rootCertificateIssuerName"></param>
    /// <param name="userTrustRootCertificate">
    ///     Should fake HTTPS certificate be trusted by this machine's user certificate
    ///     store?
    /// </param>
    /// <param name="machineTrustRootCertificate">Should fake HTTPS certificate be trusted by this machine's certificate store?</param>
    /// <param name="trustRootCertificateAsAdmin">
    ///     Should we attempt to trust certificates with elevated permissions by
    ///     prompting for UAC if required?
    /// </param>
    /// <param name="exceptionFunc"></param>
    internal CertificateManager(string? rootCertificateName, string? rootCertificateIssuerName,
        bool userTrustRootCertificate, bool machineTrustRootCertificate, bool trustRootCertificateAsAdmin,
        ExceptionHandler? exceptionFunc)
    {
        ExceptionFunc = exceptionFunc;

        UserTrustRoot = userTrustRootCertificate || machineTrustRootCertificate;

        MachineTrustRoot = machineTrustRootCertificate;
        TrustRootAsAdministrator = trustRootCertificateAsAdmin;

        if (rootCertificateName != null) RootCertificateName = rootCertificateName;

        if (rootCertificateIssuerName != null) RootCertificateIssuerName = rootCertificateIssuerName;

        CertificateEngine = CertificateEngine.BouncyCastle;
    }

    private ICertificateMaker CertEngine
    {
        get
        {
            if (certEngineValue == null)
                switch (engine)
                {
                    case CertificateEngine.BouncyCastle:
                        certEngineValue = new BcCertificateMaker(ExceptionFunc, CertificateValidDays);
                        break;
                    case CertificateEngine.BouncyCastleFast:
                        certEngineValue = new BcCertificateMakerFast(ExceptionFunc, CertificateValidDays);
                        break;
                    case CertificateEngine.DefaultWindows:
                    default:
                        certEngineValue = new WinCertificateMaker(ExceptionFunc, CertificateValidDays);
                        break;
                }

            return certEngineValue;
        }
    }

    /// <summary>
    ///     Is the root certificate used by this proxy is valid?
    /// </summary>
    internal bool CertValidated => RootCertificate != null;

    /// <summary>
    ///     Trust the RootCertificate used by this proxy server for current user
    /// </summary>
    internal bool UserTrustRoot { get; set; }

    /// <summary>
    ///     Trust the RootCertificate used by this proxy server for current machine
    ///     Needs elevated permission, otherwise will fail silently.
    /// </summary>
    internal bool MachineTrustRoot { get; set; }

    /// <summary>
    ///     Whether trust operations should be done with elevated privileges
    ///     Will prompt with UAC if required. Works only on Windows.
    /// </summary>
    internal bool TrustRootAsAdministrator { get; set; }

    /// <summary>
    ///     Exception handler
    /// </summary>
    internal ExceptionHandler? ExceptionFunc { get; set; }

    /// <summary>
    ///     Select Certificate Engine.
    ///     Optionally set to BouncyCastle.
    ///     Mono only support BouncyCastle and it is the default.
    /// </summary>
    public CertificateEngine CertificateEngine
    {
        get => engine;
        set
        {
            // For Mono (or Non-Windows) only Bouncy Castle is supported
            if (!RunTime.IsWindows) value = CertificateEngine.BouncyCastle;

            if (value != engine)
            {
                certEngineValue = null!;
                engine = value;
            }
        }
    }

    /// <summary>
    ///     Password of the Root certificate file.
    ///     <para>Set a password for the .pfx file</para>
    /// </summary>
    public string PfxPassword { get; set; } = string.Empty;

    /// <summary>
    ///     Name(path) of the Root certificate file.
    ///     <para>
    ///         Set the name(path) of the .pfx file. If it is string.Empty Root certificate file will be named as
    ///         "rootCert.pfx" (and will be saved in proxy dll directory)
    ///     </para>
    /// </summary>
    public string PfxFilePath { get; set; } = string.Empty;

    /// <summary>
    ///     Number of Days generated HTTPS certificates are valid for.
    ///     Maximum allowed on iOS 13 is 825 days and it is the default.
    /// </summary>
    public int CertificateValidDays { get; set; } = 825;

    /// <summary>
    ///     Name of the root certificate issuer.
    ///     (This is valid only when RootCertificate property is not set.)
    /// </summary>
    public string RootCertificateIssuerName
    {
        get => issuer ?? DefaultRootCertificateIssuer;
        set => issuer = value;
    }

    /// <summary>
    ///     Name of the root certificate.
    ///     (This is valid only when RootCertificate property is not set.)
    ///     If no certificate is provided then a default Root Certificate will be created and used.
    ///     The provided root certificate will be stored in proxy exe directory with the private key.
    ///     Root certificate file will be named as "rootCert.pfx".
    /// </summary>
    public string RootCertificateName
    {
        get => rootCertificateName ?? DefaultRootRootCertificateName;
        set => rootCertificateName = value;
    }

    /// <summary>
    ///     The root certificate.
    /// </summary>
    public X509Certificate2? RootCertificate
    {
        get => rootCertificate;
        set
        {
            ClearRootCertificate();
            rootCertificate = value;
        }
    }

    /// <summary>
    ///     Save all fake certificates using <seealso cref="CertificateStorage" />.
    ///     <para>for can load the certificate and not make new certificate every time. </para>
    /// </summary>
    public bool SaveFakeCertificates { get; set; } = false;

    /// <summary>
    ///     The fake certificate cache storage.
    ///     The default cache storage implementation saves certificates in folder "crts" (will be created in proxy dll
    ///     directory).
    ///     Implement ICertificateCache interface and assign concrete class here to customize.
    /// </summary>
    public ICertificateCache CertificateStorage
    {
        get => certificateCache;
        set => certificateCache = value ?? new DefaultCertificateDiskCache();
    }

    /// <summary>
    ///     Overwrite Root certificate file.
    ///     <para>true : replace an existing .pfx file if password is incorrect or if RootCertificate = null.</para>
    /// </summary>
    public bool OverwritePfxFile { get; set; } = true;

    /// <summary>
    ///     Minutes certificates should be kept in cache when not used.
    /// </summary>
    public int CertificateCacheTimeOutMinutes { get; set; } = 60;

    /// <summary>
    ///     Adjust behaviour when certificates are saved to filesystem.
    /// </summary>
    public X509KeyStorageFlags StorageFlag { get; set; } = X509KeyStorageFlags.Exportable;

    /// <summary>
    ///     Disable wild card certificates. Disabled by default.
    /// </summary>
    public bool DisableWildCardCertificates { get; set; } = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     For CertificateEngine.DefaultWindows to work we need to also check in personal store
    /// </summary>
    /// <param name="storeLocation"></param>
    /// <returns></returns>
    private bool RootCertificateInstalled(StoreLocation storeLocation)
    {
        if (RootCertificate == null) throw new Exception("Root certificate is null.");

        var value = $"{RootCertificate.Issuer}";
        return FindCertificates(StoreName.Root, storeLocation, value).Count > 0
               && (CertificateEngine != CertificateEngine.DefaultWindows
                   || FindCertificates(StoreName.My, storeLocation, value).Count > 0);
    }

    private static X509Certificate2Collection FindCertificates(StoreName storeName, StoreLocation storeLocation,
        string findValue)
    {
        var x509Store = new X509Store(storeName, storeLocation);
        try
        {
            x509Store.Open(OpenFlags.OpenExistingOnly);
            return x509Store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, findValue, false);
        }
        finally
        {
            x509Store.Close();
        }
    }

    /// <summary>
    ///     Make current machine trust the Root Certificate used by this proxy
    /// </summary>
    /// <param name="storeName"></param>
    /// <param name="storeLocation"></param>
    private void InstallCertificate(StoreName storeName, StoreLocation storeLocation)
    {
        if (RootCertificate == null) throw new Exception("Could not install certificate as it is null or empty.");

        var x509Store = new X509Store(storeName, storeLocation);

        // todo
        // also it should do not duplicate if certificate already exists
        try
        {
            x509Store.Open(OpenFlags.ReadWrite);
            x509Store.Add(RootCertificate);
        }
        catch (Exception e)
        {
            OnException(
                new Exception("Failed to make system trust root certificate "
                              + $" for {storeName}\\{storeLocation} store location. You may need admin rights.",
                    e));
        }
        finally
        {
            x509Store.Close();
        }
    }

    /// <summary>
    ///     Remove the Root Certificate trust
    /// </summary>
    /// <param name="storeName"></param>
    /// <param name="storeLocation"></param>
    /// <param name="certificate"></param>
    private void UninstallCertificate(StoreName storeName, StoreLocation storeLocation, X509Certificate2? certificate)
    {
        if (certificate == null)
        {
            OnException(new Exception("Could not remove certificate as it is null or empty."));
            return;
        }

        var x509Store = new X509Store(storeName, storeLocation);

        try
        {
            x509Store.Open(OpenFlags.ReadWrite);

            x509Store.Remove(certificate);
        }
        catch (Exception e)
        {
            OnException(new Exception("Failed to remove root certificate trust "
                                      + $" for {storeLocation} store location. You may need admin rights.", e));
        }
        finally
        {
            x509Store.Close();
        }
    }

    private X509Certificate2 MakeCertificate(string certificateName, bool isRootCertificate)
    {
        //if (isRoot != (null == signingCertificate))
        //{
        //    throw new ArgumentException(
        //        "You must specify a Signing Certificate if and only if you are not creating a root.",
        //        nameof(signingCertificate));
        //}

        if (!isRootCertificate && RootCertificate == null) CreateRootCertificate();

        var certificate = CertEngine.MakeCertificate(certificateName, isRootCertificate ? null : RootCertificate);

        if (CertificateEngine == CertificateEngine.DefaultWindows)
            Task.Run(() => UninstallCertificate(StoreName.My, StoreLocation.CurrentUser, certificate));

        return certificate;
    }

    private void OnException(Exception exception)
    {
        ExceptionFunc?.Invoke(exception);
    }

    /// <summary>
    ///     Create an SSL certificate
    /// </summary>
    /// <param name="certificateName"></param>
    /// <param name="isRootCertificate"></param>
    /// <returns></returns>
    internal X509Certificate2? CreateCertificate(string certificateName, bool isRootCertificate)
    {
        X509Certificate2? certificate;
        try
        {
            if (!isRootCertificate && SaveFakeCertificates)
            {
                var subjectName = ProxyConstants.CnRemoverRegex
                    .Replace(certificateName, string.Empty)
                    .Replace("*", "$x$");

                try
                {
                    certificate = certificateCache.LoadCertificate(subjectName, StorageFlag);

                    if (certificate != null && certificate.NotAfter <= DateTime.Now)
                    {
                        OnException(new Exception($"Cached certificate for {subjectName} has expired."));
                        certificate = null;
                    }
                }
                catch (Exception e)
                {
                    OnException(new Exception("Failed to load fake certificate.", e));
                    certificate = null;
                }

                if (certificate == null)
                {
                    certificate = MakeCertificate(certificateName, false);

                    //Don't need to wait for save to complete
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            var lockKey = subjectName.ToLower();
                            //acquire lock by subjectName
                            //Async lock is not needed. Since this is a rare race-condition
                            lock (_saveCertificateLocks.GetOrAdd(lockKey, new object()))
                            {
                                try
                                {
                                    //no two tasks with same subject name should together enter here 
                                    certificateCache.SaveCertificate(subjectName, certificate);
                                }
                                finally
                                {
                                    //save operation is complete. Free lock from memory.
                                    _saveCertificateLocks.TryRemove(lockKey, out var _);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            OnException(new Exception("Failed to save fake certificate.", e));
                        }
                    });
                }
            }
            else
            {
                certificate = MakeCertificate(certificateName, isRootCertificate);
            }
        }
        catch (Exception e)
        {
            OnException(e);
            certificate = null;
        }

        return certificate;
    }

    /// <summary>
    ///     Creates a server certificate signed by the root certificate.
    /// </summary>
    /// <param name="certificateName"></param>
    /// <returns></returns>
    public async Task<X509Certificate2?> CreateServerCertificate(string certificateName)
    {
        // check in cache first
        if (cachedCertificates.TryGetValue(certificateName, out var cached))
        {
            cached.LastAccess = DateTime.UtcNow;
            return cached.Certificate;
        }

        var createdTask = false;
        Task<X509Certificate2?> createCertificateTask;
        await pendingCertificateCreationTaskLock.WaitAsync();
        try
        {
            // check in cache first
            if (cachedCertificates.TryGetValue(certificateName, out cached))
            {
                cached.LastAccess = DateTime.UtcNow;
                return cached.Certificate;
            }

            // handle burst requests with same certificate name
            // by checking for existing task for same certificate name
            if (!pendingCertificateCreationTasks.TryGetValue(certificateName, out createCertificateTask))
            {
                // run certificate creation task & add it to pending tasks
                createCertificateTask = Task.Run(() =>
                {
                    var result = CreateCertificate(certificateName, false);
                    if (result != null) cachedCertificates.TryAdd(certificateName, new CachedCertificate(result));

                    return result;
                });

                pendingCertificateCreationTasks[certificateName] = createCertificateTask;
                createdTask = true;
            }
        }
        finally
        {
            pendingCertificateCreationTaskLock.Release();
        }

        var certificate = await createCertificateTask;

        if (createdTask)
        {
            // cleanup pending task
            await pendingCertificateCreationTaskLock.WaitAsync();
            try
            {
                pendingCertificateCreationTasks.Remove(certificateName);
            }
            finally
            {
                pendingCertificateCreationTaskLock.Release();
            }
        }

        return certificate;
    }

    /// <summary>
    ///     A method to clear outdated certificates
    /// </summary>
    internal async void ClearIdleCertificates()
    {
        var cancellationToken = clearCertificatesTokenSource.Token;
        while (!cancellationToken.IsCancellationRequested)
        {
            var cutOff = DateTime.UtcNow.AddMinutes(-CertificateCacheTimeOutMinutes);

            var outdated = cachedCertificates.Where(x => x.Value.LastAccess < cutOff).ToList();

            foreach (var cache in outdated) cachedCertificates.TryRemove(cache.Key, out _);

            // after a minute come back to check for outdated certificates in cache
            try
            {
                await Task.Delay(1000 * 60, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    ///     Stops the certificate cache clear process
    /// </summary>
    internal void StopClearIdleCertificates()
    {
        clearCertificatesTokenSource.Cancel();
    }

    /// <summary>
    ///     Attempts to create a RootCertificate.
    /// </summary>
    /// <param name="persistToFile">if set to <c>true</c> try to load/save the certificate from rootCert.pfx.</param>
    /// <returns>
    ///     true if succeeded, else false.
    /// </returns>
    public bool CreateRootCertificate(bool persistToFile = true)
    {
        lock (rootCertCreationLock)
        {
            if (persistToFile && RootCertificate == null) RootCertificate = LoadRootCertificate();

            if (RootCertificate != null) return true;

            if (!OverwritePfxFile)
                try
                {
                    var rootCert = certificateCache.LoadRootCertificate(PfxFilePath, PfxPassword,
                        X509KeyStorageFlags.Exportable);

                    if (rootCert != null && rootCert.NotAfter <= DateTime.Now)
                    {
                        OnException(new Exception("Loaded root certificate has expired."));
                        return false;
                    }

                    if (rootCert != null)
                    {
                        RootCertificate = rootCert;
                        return true;
                    }
                }
                catch (Exception e)
                {
                    // root cert cannot be loaded
                    OnException(new Exception("Root cert cannot be loaded.", e));
                }

            try
            {
                RootCertificate = CreateCertificate(RootCertificateName, true);
            }
            catch (Exception e)
            {
                OnException(e);
            }

            if (persistToFile && RootCertificate != null)
                try
                {
                    try
                    {
                        certificateCache.Clear();
                    }
                    catch (Exception e)
                    {
                        OnException(new Exception("An error happened when clearing certificate cache.", e));
                    }

                    certificateCache.SaveRootCertificate(PfxFilePath, PfxPassword, RootCertificate);
                }
                catch (Exception e)
                {
                    OnException(e);
                }

            return RootCertificate != null;
        }
    }

    /// <summary>
    ///     Loads root certificate from current executing assembly location with expected name rootCert.pfx.
    /// </summary>
    /// <returns></returns>
    public X509Certificate2? LoadRootCertificate()
    {
        try
        {
            var rootCert =
                certificateCache.LoadRootCertificate(PfxFilePath, PfxPassword, X509KeyStorageFlags.Exportable);

            if (rootCert != null && rootCert.NotAfter <= DateTime.Now)
            {
                OnException(new ArgumentException("Loaded root certificate has expired."));
                return null;
            }

            return rootCert;
        }
        catch (Exception e)
        {
            OnException(e);
            return null;
        }
    }

    /// <summary>
    ///     Manually load a Root certificate file from give path (.pfx file).
    /// </summary>
    /// <param name="pfxFilePath">
    ///     Set the name(path) of the .pfx file. If it is string.Empty Root certificate file will be
    ///     named as "rootCert.pfx" (and will be saved in proxy dll directory).
    /// </param>
    /// <param name="password">Set a password for the .pfx file.</param>
    /// <param name="overwritePfXFile">
    ///     true : replace an existing .pfx file if password is incorrect or if
    ///     RootCertificate==null.
    /// </param>
    /// <param name="storageFlag"></param>
    /// <returns>
    ///     true if succeeded, else false.
    /// </returns>
    public bool LoadRootCertificate(string pfxFilePath, string password, bool overwritePfXFile = true,
        X509KeyStorageFlags storageFlag = X509KeyStorageFlags.Exportable)
    {
        PfxFilePath = pfxFilePath;
        PfxPassword = password;
        OverwritePfxFile = overwritePfXFile;
        StorageFlag = storageFlag;

        RootCertificate = LoadRootCertificate();

        return RootCertificate != null;
    }

    /// <summary>
    ///     Trusts the root certificate in user store, optionally also in machine store.
    ///     Machine trust would require elevated permissions (will silently fail otherwise).
    /// </summary>
    public void TrustRootCertificate(bool machineTrusted = false)
    {
        // currentUser\personal
        InstallCertificate(StoreName.My, StoreLocation.CurrentUser);

        if (!machineTrusted)
        {
            // currentUser\Root
            InstallCertificate(StoreName.Root, StoreLocation.CurrentUser);
        }
        else
        {
            // current system
            InstallCertificate(StoreName.My, StoreLocation.LocalMachine);

            // this adds to both currentUser\Root & currentMachine\Root
            InstallCertificate(StoreName.Root, StoreLocation.LocalMachine);
        }
    }

    /// <summary>
    ///     Puts the certificate to the user store, optionally also to machine store.
    ///     Prompts with UAC if elevated permissions are required. Works only on Windows.
    /// </summary>
    /// <returns>True if success.</returns>
    public bool TrustRootCertificateAsAdmin(bool machineTrusted = false)
    {
        if (!RunTime.IsWindows) return false;

        // currentUser\Personal
        InstallCertificate(StoreName.My, StoreLocation.CurrentUser);

        var pfxFileName = Path.GetTempFileName();
        File.WriteAllBytes(pfxFileName, RootCertificate!.Export(X509ContentType.Pkcs12, PfxPassword));

        // currentUser\Root, currentMachine\Personal &  currentMachine\Root
        var info = new ProcessStartInfo
        {
            FileName = "certutil.exe",
            CreateNoWindow = true,
            UseShellExecute = true,
            Verb = "runas",
            ErrorDialog = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        if (!machineTrusted)
            info.Arguments = "-f -user -p \"" + PfxPassword + "\" -importpfx root \"" + pfxFileName + "\"";
        else
            info.Arguments = "-importPFX -p \"" + PfxPassword + "\" -f \"" + pfxFileName + "\"";

        try
        {
            var process = Process.Start(info);
            if (process == null) return false;

            process.WaitForExit();
            File.Delete(pfxFileName);
        }
        catch (Exception e)
        {
            OnException(e);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Ensure certificates are setup (creates root if required).
    ///     Also makes root certificate trusted based on initial setup from proxy constructor for user/machine trust.
    /// </summary>
    public void EnsureRootCertificate()
    {
        if (!CertValidated) CreateRootCertificate();

        if (TrustRootAsAdministrator)
            TrustRootCertificateAsAdmin(MachineTrustRoot);
        else if (UserTrustRoot) TrustRootCertificate(MachineTrustRoot);
    }

    /// <summary>
    ///     Ensure certificates are setup (creates root if required).
    ///     Also makes root certificate trusted based on provided parameters.
    ///     Note:setting machineTrustRootCertificate to true will force userTrustRootCertificate to true.
    /// </summary>
    /// <param name="userTrustRootCertificate">
    ///     Should fake HTTPS certificate be trusted by this machine's user certificate
    ///     store?
    /// </param>
    /// <param name="machineTrustRootCertificate">Should fake HTTPS certificate be trusted by this machine's certificate store?</param>
    /// <param name="trustRootCertificateAsAdmin">
    ///     Should we attempt to trust certificates with elevated permissions by
    ///     prompting for UAC if required?
    /// </param>
    public void EnsureRootCertificate(bool userTrustRootCertificate,
        bool machineTrustRootCertificate, bool trustRootCertificateAsAdmin = false)
    {
        UserTrustRoot = userTrustRootCertificate || machineTrustRootCertificate;
        MachineTrustRoot = machineTrustRootCertificate;
        TrustRootAsAdministrator = trustRootCertificateAsAdmin;

        EnsureRootCertificate();
    }

    /// <summary>
    ///     Determines whether the root certificate is trusted.
    /// </summary>
    public bool IsRootCertificateUserTrusted()
    {
        return RootCertificateInstalled(StoreLocation.CurrentUser) || IsRootCertificateMachineTrusted();
    }

    /// <summary>
    ///     Determines whether the root certificate is machine trusted.
    /// </summary>
    public bool IsRootCertificateMachineTrusted()
    {
        return RootCertificateInstalled(StoreLocation.LocalMachine);
    }

    /// <summary>
    ///     Removes the trusted certificates from user store, optionally also from machine store.
    ///     To remove from machine store elevated permissions are required (will fail silently otherwise).
    /// </summary>
    /// <param name="machineTrusted">Should also remove from machine store?</param>
    public void RemoveTrustedRootCertificate(bool machineTrusted = false)
    {
        // currentUser\personal
        UninstallCertificate(StoreName.My, StoreLocation.CurrentUser, RootCertificate);

        if (!machineTrusted)
        {
            // currentUser\Root
            UninstallCertificate(StoreName.Root, StoreLocation.CurrentUser, RootCertificate);
        }
        else
        {
            // current system
            UninstallCertificate(StoreName.My, StoreLocation.LocalMachine, RootCertificate);

            // this adds to both currentUser\Root & currentMachine\Root
            UninstallCertificate(StoreName.Root, StoreLocation.LocalMachine, RootCertificate);
        }
    }

    /// <summary>
    ///     Removes the trusted certificates from user store, optionally also from machine store
    /// </summary>
    /// <returns>Should also remove from machine store?</returns>
    public bool RemoveTrustedRootCertificateAsAdmin(bool machineTrusted = false)
    {
        if (!RunTime.IsWindows) return false;

        // currentUser\Personal
        UninstallCertificate(StoreName.My, StoreLocation.CurrentUser, RootCertificate);

        var infos = new List<ProcessStartInfo>();
        if (!machineTrusted)
            infos.Add(new ProcessStartInfo
            {
                FileName = "certutil.exe",
                Arguments = "-delstore -user Root \"" + RootCertificateName + "\"",
                CreateNoWindow = true,
                UseShellExecute = true,
                Verb = "runas",
                ErrorDialog = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        else
            infos.AddRange(
                new List<ProcessStartInfo>
                {
                    // currentMachine\Personal
                    new()
                    {
                        FileName = "certutil.exe",
                        Arguments = "-delstore My \"" + RootCertificateName + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = true,
                        Verb = "runas",
                        ErrorDialog = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    },

                    // currentUser\Personal & currentMachine\Personal
                    new()
                    {
                        FileName = "certutil.exe",
                        Arguments = "-delstore Root \"" + RootCertificateName + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = true,
                        Verb = "runas",
                        ErrorDialog = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                });

        var success = true;
        try
        {
            foreach (var info in infos)
            {
                var process = Process.Start(info);

                if (process == null) success = false;

                process?.WaitForExit();
            }
        }
        catch
        {
            success = false;
        }

        return success;
    }

    /// <summary>
    ///     Clear the root certificate and cache.
    /// </summary>
    public void ClearRootCertificate()
    {
        certificateCache.Clear();
        cachedCertificates.Clear();
        rootCertificate = null;
    }

    private void Dispose(bool disposing)
    {
        if (disposed) return;

        if (disposing) clearCertificatesTokenSource.Dispose();

        disposed = true;
    }

    ~CertificateManager()
    {
        Dispose(false);
    }
}