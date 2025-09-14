using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    private string sessionID;
    private bool alreadyInitialized = false;
    private ListenerRegistration sessionListener;

#if UNITY_EDITOR
    private bool isEditor => true;
#else
    private bool isEditor => false;
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize Firebase safely
            FirebaseInit init = FindFirstObjectByType<FirebaseInit>();
            if (init != null && init.Ready)
            {
                auth = init.auth;
                db = init.db;
            }
            else
            {
                Debug.LogError("Firebase not ready. Make sure FirebaseInit runs first!");
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

#if UNITY_EDITOR
    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && auth?.CurrentUser != null)
        {
            _ = ClearSessionEditorAsync(auth.CurrentUser.UserId);
        }
    }

    private async Task ClearSessionEditorAsync(string uid)
    {
        try
        {
            var docRef = db.Collection("users").Document(uid);
            await docRef.UpdateAsync(
                new Dictionary<string, object> { { "currentSessionId", null } }
            );
            Debug.Log($"[Editor Stop] Cleared session for {uid}");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Editor Stop] failed clearing session: " + e.Message);
        }
        finally
        {
            sessionID = null;
        }
    }
#endif

    private async void Start()
    {
        if (alreadyInitialized)
            return;
        alreadyInitialized = true;

        while (FirebaseInit.Instance == null || !FirebaseInit.Instance.Ready)
            await Task.Delay(50);

        auth = FirebaseInit.Instance.auth;
        db = FirebaseInit.Instance.db;

        if (auth == null || db == null)
        {
            Debug.LogError("Firebase not initialized in AuthManager!");
            return;
        }

        if (auth.CurrentUser != null)
            await SafeCall(async () => await TryAutoLogin(auth.CurrentUser.UserId));
        else
            GoToLoginScene();
    }

    public void RegisterUser(string email, string password, string username)
    {
        _ = SafeCall(async () =>
        {
            var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            var newUser = result.User;
            if (newUser == null)
                throw new Exception("RegisterUser: result.User is null");

            var docRef = db.Collection("users").Document(newUser.UserId);
            var userData = new Dictionary<string, object>
            {
                { "email", email },
                { "username", username },
                { "XP", 0 },
                { "kills", 0 },
                { "level", 1 },
                { "createdAt", Timestamp.GetCurrentTimestamp() },
                { "lastLogin", Timestamp.GetCurrentTimestamp() },
                { "currentSessionId", null },
            };
            await docRef.SetAsync(userData, SetOptions.MergeAll);
            Debug.Log($"✅ Registered and created profile for {email}");
        });
    }

    public void LoginUser(string email, string password)
    {
        if (auth == null || db == null)
        {
            Debug.LogError("Firebase not initialized yet!");
            return;
        }

        _ = SafeCall(async () =>
        {
            var authResult = await auth.SignInWithEmailAndPasswordAsync(email, password);
            var user = authResult.User;
            if (user == null)
                throw new Exception("LoginUser: user is null after SignIn");

            var docRef = db.Collection("users").Document(user.UserId);
            var snapshot = await docRef.GetSnapshotAsync(Source.Server);
            snapshot.TryGetValue("currentSessionId", out string activeSession);

            if (!string.IsNullOrEmpty(activeSession))
            {
                Debug.LogWarning(
                    $"⚠️ Login blocked: account active on another device. Firestore SessionID: {activeSession}"
                );
                auth.SignOut();
                sessionID = null;
                RunOnMainThread(() => GoToLoginScene());
                return;
            }

            sessionID = Guid.NewGuid().ToString();
            bool claimed = await ClaimSessionIfFree(docRef, user.UserId);
            if (!claimed)
            {
                auth.SignOut();
                sessionID = null;
                RunOnMainThread(() => GoToLoginScene());
                return;
            }

            RunOnMainThread(() =>
            {
                Debug.Log($"✅ Logged in: {user.Email} SessionID: {sessionID}");
                SceneManager.LoadScene("Character Selection");
            });
        });
    }

    private async Task<bool> ClaimSessionIfFree(DocumentReference docRef, string uid)
    {
        var snapshot = await docRef.GetSnapshotAsync(Source.Server);
        snapshot.TryGetValue("currentSessionId", out string current);
        if (!string.IsNullOrEmpty(current))
            return false;

        await docRef.SetAsync(
            new Dictionary<string, object>
            {
                { "currentSessionId", sessionID },
                { "lastLogin", Timestamp.GetCurrentTimestamp() },
            },
            SetOptions.MergeAll
        );

        StartSessionListener(uid);
        return true;
    }

    private async Task TryAutoLogin(string uid)
    {
        var docRef = db.Collection("users").Document(uid);
        var snapshot = await docRef.GetSnapshotAsync(Source.Server);
        snapshot.TryGetValue("currentSessionId", out string activeSession);

        if (!string.IsNullOrEmpty(activeSession))
        {
            Debug.LogWarning($"⚠️ Auto-login blocked: another session active: {activeSession}");
            RunOnMainThread(() => GoToLoginScene());
            return;
        }

        sessionID = Guid.NewGuid().ToString();
        bool claimed = await ClaimSessionIfFree(docRef, uid);
        if (!claimed)
        {
            RunOnMainThread(() => GoToLoginScene());
            return;
        }

        RunOnMainThread(() =>
        {
            Debug.Log("✅ Auto-login success");
            SceneManager.LoadScene("Character Selection");
        });
    }

    private void StartSessionListener(string uid)
    {
        sessionListener?.Stop();
        sessionListener = db.Collection("users")
            .Document(uid)
            .Listen(snapshot =>
            {
                if (
                    snapshot.Exists
                    && snapshot.TryGetValue("currentSessionId", out string activeSession)
                )
                {
                    if (string.IsNullOrEmpty(sessionID))
                        return;
                    if (activeSession != sessionID && !string.IsNullOrEmpty(activeSession))
                    {
                        Debug.LogWarning(
                            $"⚠️ Session expired (remote login). Firestore: {activeSession}, Local: {sessionID}"
                        );
                        RunOnMainThread(() => Logout());
                    }
                }
            });
    }

    public void Logout()
    {
        _ = SafeCall(async () =>
        {
            var user = auth.CurrentUser;
            if (user != null && !string.IsNullOrEmpty(sessionID))
            {
                var docRef = db.Collection("users").Document(user.UserId);
                var snapshot = await docRef.GetSnapshotAsync(Source.Server);
                snapshot.TryGetValue("currentSessionId", out string activeSession);

                if (activeSession == sessionID)
                {
                    await docRef.UpdateAsync(
                        new Dictionary<string, object> { { "currentSessionId", null } }
                    );
                    Debug.Log($"✅ Firestore session cleared for {user.UserId}");
                }
            }

            sessionListener?.Stop();
            sessionListener = null;
            auth.SignOut();
            sessionID = null;
            RunOnMainThread(() => GoToLoginScene());
            Debug.Log("✅ Local logout completed");
        });
    }

    private void GoToLoginScene() => SceneManager.LoadScene("SignUp&Login");

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SignUp&Login")
        {
            var ui = FindFirstObjectByType<LoginSignUp_UI>();
            ui?.loginPanel?.SetActive(true);
        }
    }

    private async void OnApplicationQuit()
    {
        if (auth?.CurrentUser != null && !string.IsNullOrEmpty(sessionID))
        {
            try
            {
                var uid = auth.CurrentUser.UserId;
                var docRef = db.Collection("users").Document(uid);
                await docRef.UpdateAsync(
                    new Dictionary<string, object> { { "currentSessionId", null } }
                );
                Debug.Log("[Quit] Cleared session in Firestore.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Quit] failed to clear session: " + e.Message);
            }
        }
    }

    private void RunOnMainThread(Action action)
    {
#if UNITY_EDITOR
        EditorApplication.delayCall += () =>
        {
            action();
        };
#else
        UnityMainThreadDispatcher.Instance().Enqueue(action);
#endif
    }

    private async Task SafeCall(Func<Task> func)
    {
        try
        {
            await func();
        }
        catch (Exception e)
        {
            Debug.LogError("AuthManager async exception: " + e);
        }
    }
}

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance;

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            var obj = new GameObject("MainThreadDispatcher");
            _instance = obj.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(obj);
        }
        return _instance;
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    private void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
                _executionQueue.Dequeue().Invoke();
        }
    }
}
