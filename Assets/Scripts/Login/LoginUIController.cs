using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Photon.Pun; // <- para establecer NickName después del login

public class LoginUIController : MonoBehaviour
{
    [Header("Refs UI")]
    [SerializeField] private TMP_InputField inputUsuarioEmail;
    [SerializeField] private TMP_InputField inputPassword;
    [SerializeField] private Button btnIngresar;
    [SerializeField] private Button btnRegresar;
    [SerializeField] private TextMeshProUGUI lblEstado;

    [Header("Photon")]
    [SerializeField] private ConnectToServer photonConnector; // arrástralo en el Inspector

    [Header("API")]
    [SerializeField] private string apiBaseUrl = "http://127.0.0.1:8000";
    [SerializeField] private float requestTimeoutSeconds = 15f;

    [Header("Opcional")]
    [SerializeField] private bool rememberToken = true; // guarda token en PlayerPrefs

    // Claves PlayerPrefs (para coherencia con AuthState)
    private const string PP_TOKEN = "auth.token";
    private const string PP_USER = "auth.user";

    [Serializable] private class LoginPayload { public string email; public string password; }
    [Serializable] private class UserDTO { public int id; public string username; public string email; }
    [Serializable] private class LoginResponse { public string token; public UserDTO user; }

    private void Awake()
    {
        if (btnIngresar) btnIngresar.onClick.AddListener(OnClickIngresar);
        if (btnRegresar) btnRegresar.onClick.AddListener(OnClickRegresar);
        if (lblEstado) lblEstado.text = "";

        // (Opcional) Autologin si ya hay token guardado
        if (rememberToken && AuthState.TryLoadFromPrefs())
        {
            SetEstado($"Bienvenido de nuevo, {AuthState.Username}.");
            // Si quieres conectar a Photon automáticamente cuando hay token:
            if (photonConnector != null)
            {
                PhotonNetwork.NickName = string.IsNullOrWhiteSpace(AuthState.Username) ? "Player" : AuthState.Username;
                photonConnector.conectarServidor();
            }
        }
    }

    private void OnDestroy()
    {
        if (btnIngresar) btnIngresar.onClick.RemoveListener(OnClickIngresar);
        if (btnRegresar) btnRegresar.onClick.RemoveListener(OnClickRegresar);
    }

    public void OnClickIngresar()
    {
        var email = inputUsuarioEmail ? inputUsuarioEmail.text.Trim() : "";
        var pass = inputPassword ? inputPassword.text : "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            SetEstado("Completa usuario (email) y contraseña.");
            return;
        }

        StartCoroutine(CoLogin(email, pass));
    }

    private IEnumerator CoLogin(string email, string password)
    {
        SetEstado("Conectando...");
        ToggleInteractable(false);

        var payload = new LoginPayload { email = email, password = password };
        var json = JsonUtility.ToJson(payload);
        var url = apiBaseUrl.TrimEnd('/') + "/auth/login";

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = Mathf.RoundToInt(requestTimeoutSeconds);

        yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        bool hasError = req.result != UnityWebRequest.Result.Success || req.responseCode < 200 || req.responseCode >= 300;
#else
        bool hasError = req.isNetworkError || req.isHttpError || req.responseCode < 200 || req.responseCode >= 300;
#endif

        if (hasError)
        {
            SetEstado($"Error {req.responseCode}: {req.downloadHandler.text}");
            ToggleInteractable(true);
            yield break;
        }

        var body = req.downloadHandler.text;
        LoginResponse resp = null;
        try
        {
            resp = JsonUtility.FromJson<LoginResponse>(body);
        }
        catch (Exception e)
        {
            SetEstado("Respuesta inválida del servidor.");
            Debug.LogWarning($"[Login] Parse error: {e}\nBody: {body}");
            ToggleInteractable(true);
            yield break;
        }

        if (resp == null || string.IsNullOrEmpty(resp.token))
        {
            SetEstado("Login fallido: sin token.");
            ToggleInteractable(true);
            yield break;
        }

        // Guardar token y usuario
        AuthState.SetToken(resp.token, resp.user?.username, resp.user?.email, rememberToken);

        // Si tu EndorsementUploader expone SetAuthToken, pásale el Bearer
        if (EndorsementUploader.Instance != null)
        {
            // Este método debe existir en tu Uploader (como vimos antes).
            EndorsementUploader.Instance.SetAuthToken(resp.token);
        }

        // ✅ Ahora sí: conectamos a Photon (ya autenticado)
        if (photonConnector != null)
        {
            PhotonNetwork.NickName = string.IsNullOrWhiteSpace(AuthState.Username) ? "Player" : AuthState.Username;
            photonConnector.conectarServidor();
        }
        else
        {
            Debug.LogWarning("[Login] No se asignó ConnectToServer en el Inspector.");
        }

        SetEstado("¡Login correcto! Conectando a servidor...");
        ToggleInteractable(true);
    }

    private void ToggleInteractable(bool on)
    {
        if (btnIngresar) btnIngresar.interactable = on;
        if (inputUsuarioEmail) inputUsuarioEmail.interactable = on;
        if (inputPassword) inputPassword.interactable = on;
    }

    private void SetEstado(string msg)
    {
        if (lblEstado) lblEstado.text = msg;
        Debug.Log($"[Login] {msg}");
    }

    // Opcional: botón "Regresar" para cerrar/ocultar esta UI o cambiar de escena
    private void OnClickRegresar()
    {
        // Aquí puedes hacer SetActive(false) del panel, cargar otra escena, etc.
        gameObject.SetActive(false);
    }
}

/// <summary>
/// Estado de autenticación en memoria (y persistencia opcional con PlayerPrefs).
/// </summary>
public static class AuthState
{
    public static string Token { get; private set; }
    public static string Username { get; private set; }
    public static string Email { get; private set; }

    private const string PP_TOKEN = "auth.token";
    private const string PP_USER = "auth.user";

    public static void SetToken(string token, string username, string email, bool persist)
    {
        Token = token;
        Username = username;
        Email = email;

        if (persist)
        {
            PlayerPrefs.SetString(PP_TOKEN, token);
            PlayerPrefs.SetString(PP_USER, username ?? "");
            PlayerPrefs.Save();
        }
    }

    public static bool TryLoadFromPrefs()
    {
        if (!PlayerPrefs.HasKey(PP_TOKEN)) return false;
        Token = PlayerPrefs.GetString(PP_TOKEN, "");
        Username = PlayerPrefs.GetString(PP_USER, "");
        return !string.IsNullOrEmpty(Token);
    }

    public static void Clear()
    {
        Token = null;
        Username = null;
        Email = null;
        PlayerPrefs.DeleteKey(PP_TOKEN);
        PlayerPrefs.DeleteKey(PP_USER);
    }
}
