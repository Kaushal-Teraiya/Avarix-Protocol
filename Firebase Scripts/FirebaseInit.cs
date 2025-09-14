using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class FirebaseInit : MonoBehaviour
{
    public static FirebaseInit Instance { get; private set; }
    public FirebaseAuth auth { get; private set; }
    public FirebaseFirestore db { get; private set; }
    private bool isReady = false;
    public bool Ready => isReady;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeFirebase();
    }

    private async void InitializeFirebase()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus != DependencyStatus.Available)
        {
            Debug.LogError($"Could not resolve Firebase dependencies: {dependencyStatus}");
            return;
        }

        FirebaseApp app;

        // Build uses default instance
        app = FirebaseApp.DefaultInstance;

        auth = FirebaseAuth.GetAuth(app);
        db = FirebaseFirestore.GetInstance(app);

        isReady = true;
        Debug.Log(
            $"✅ Firebase initialized successfully ({(Application.isEditor ? "Editor" : "Build")})."
        );
    }
}
