using System;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginSignUp_UI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject signUpPanel;

    [Header("Login Fields")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;

    [Header("Sign Up Fields")]
    public TMP_InputField signUpEmailInput;
    public TMP_InputField signUpPasswordInput;
    public TMP_InputField usernameInput;

    private AuthManager authManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        authManager = FindFirstObjectByType<AuthManager>();
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
    }

    public void ActivateSignUpPanel()
    {
        loginPanel.SetActive(false);
        signUpPanel.SetActive(true);
    }

    public void ActivateLoginPanel()
    {
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
    }

    public void OnLoginButton()
    {
        string email = loginEmailInput.text;
        string password = loginPasswordInput.text;
        authManager.LoginUser(email, password);
    }

    public void OnLogoutButton()
    {
        authManager.Logout();
    }

    public void OnSignUpButton()
    {
        string email = signUpEmailInput.text;
        string password = signUpPasswordInput.text;
        string username = usernameInput.text;
        authManager.RegisterUser(email, password, username);
    }

    // Update is called once per frame
    void Update() { }
}
