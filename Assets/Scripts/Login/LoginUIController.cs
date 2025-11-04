using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Photon.Pun;

public class LoginUIController : MonoBehaviour
{
    [Header("Refs UI")]
    [SerializeField] private TMP_InputField inputUsuarioEmail;
    [SerializeField] private TMP_InputField inputPassword;
    [SerializeField] private Button btnIngresar;
    [SerializeField] private Button btnRegresar;
    [SerializeField] private TextMeshProUGUI lblEstado;

    [Header("Navegacion")]
    [SerializeField] private GameObject panelAlRegresar;

    [Header("Photon")]
    [SerializeField] private NetworkManager photonConnector;

    [Header("API")]
    [SerializeField] private string apiBaseUrl = "http://127.0.0.1:8000";
    [SerializeField] private float requestTimeoutSeconds = 15f;

    [Header("Opcional")]
    [SerializeField] private bool rememberToken = true; // guarda token en PlayerPrefs

    // --- Modelos simples para (de)serializar ---
    [Serializable] private class LoginPayload { public string email; public string password; }
    [Serializable] private class UserDTO { public int id; public string username; public string email; }
    [Serializable] private class LoginResponse { public string token; public UserDTO user; }

    // formatos de error típicos de FastAPI
    [Serializable] private class DetailList { public List<DetailItem> detail; }
    [Serializable] private class DetailItem { public string msg; public string type; public List<string> loc; }
    [Serializable] private class DetailString { public string detail; }

    private void Awake()
    {
        if (btnIngresar) btnIngresar.onClick.AddListener(OnClickIngresar);
        if (btnRegresar) btnRegresar.onClick.AddListener(OnClickRegresar);
        if (lblEstado) lblEstado.text = "";

        // Recordar sesion
        if (rememberToken && AuthState.TryLoadFromPrefs())
        {
            SetEstado($"Bienvenido de nuevo, {AuthState.Username}.");
            if (photonConnector != null)
            {
                PhotonNetwork.NickName = string.IsNullOrWhiteSpace(AuthState.Username) ? "Player" : AuthState.Username;
                photonConnector.ConectarServidor();
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
        /*var email = inputUsuarioEmail ? inputUsuarioEmail.text.Trim() : "";
        var pass = inputPassword ? inputPassword.text : "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            SetEstado("Escribe tu correo y contraseña.");
            return;
        }

        StartCoroutine(CoLogin(email, pass));*/
        photonConnector.EntrarLobbyIndividual();
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
        bool hasTransportError = req.result != UnityWebRequest.Result.Success;
#else
        bool hasTransportError = req.isNetworkError || req.isHttpError;
#endif

        var status = (int)req.responseCode;
        var body = req.downloadHandler.text;

        if (hasTransportError || status < 200 || status >= 300)
        {
            SetEstado(MapFriendlyError(status, body));
            if (inputPassword) inputPassword.text = "";
            ToggleInteractable(true);
            yield break;
        }

        LoginResponse resp = null;
        try { resp = JsonUtility.FromJson<LoginResponse>(body); }
        catch (Exception e)
        {
            Debug.LogWarning($"[Login] Parse error: {e}\nBody: {body}");
            SetEstado("Respuesta inesperada. Intenta de nuevo.");
            ToggleInteractable(true);
            yield break;
        }

        if (resp == null || string.IsNullOrEmpty(resp.token))
        {
            SetEstado("No se recibio el token. Intenta de nuevo.");
            ToggleInteractable(true);
            yield break;
        }

        // Guardar token y usuario
        AuthState.SetToken(resp.token, resp.user?.username, resp.user?.email, rememberToken);

        if (EndorsementUploader.Instance != null)
        {
            EndorsementUploader.Instance.SetAuthToken(resp.token);
        }

        // Conectar a Photon
        if (photonConnector != null)
        {
            PhotonNetwork.NickName = string.IsNullOrWhiteSpace(AuthState.Username) ? "Player" : AuthState.Username;
            photonConnector.ConectarServidor();
        }
        else
        {
            Debug.LogWarning("[Login] No se asignó ConnectToServer en el Inspector.");
        }

        SetEstado("¡Listo! Conectando al servidor...");
        ToggleInteractable(true);
    }

    // Handler de msj de error
    private string MapFriendlyError(int status, string body)
    {
        string detail = null;

        try
        {
            var s = JsonUtility.FromJson<DetailString>(body);
            if (s != null && !string.IsNullOrEmpty(s.detail)) detail = s.detail;
        }
        catch { /* ignore */ }

        if (string.IsNullOrEmpty(detail))
        {
            try
            {
                var l = JsonUtility.FromJson<DetailList>(body);
                if (l != null && l.detail != null && l.detail.Count > 0 && !string.IsNullOrEmpty(l.detail[0].msg))
                    detail = l.detail[0].msg;
            }
            catch { /* ignore */ }
        }

        if (!string.IsNullOrEmpty(detail))
        {
            var lower = detail.ToLowerInvariant();

            if (lower.Contains("invalid email") || lower.Contains("correo") || lower.Contains("email"))
                return "Credenciales incorrectas.";
            if (lower.Contains("incorrect") && lower.Contains("password"))
                return "Credenciales incorrectas.";
            if (lower.Contains("user") && lower.Contains("not found"))
                return "Credenciales incorrectas.";
            if (lower.Contains("too many") || lower.Contains("rate"))
                return "Demasiados intentos. Espera un momento.";
            if (lower.Contains("password") && lower.Contains("length"))
                return "Credenciales incorrectas.";
        }

        // Fallback por codigo
        switch (status)
        {
            case 0: return "Sin conexión. Revisa tu red.";
            case 400: return string.IsNullOrEmpty(detail) ? "Datos inválidos." : detail;
            case 401: return string.IsNullOrEmpty(detail) ? "Credenciales incorrectas." : detail;
            case 403: return "Acceso no permitido.";
            case 404: return "Servicio no disponible.";
            case 422: return string.IsNullOrEmpty(detail) ? "Revisa tu correo y contraseña." : detail;
            case 429: return "Demasiados intentos. Intenta más tarde.";
            case 500: return "Error del servidor. Intenta más tarde.";
            case 503: return "Servidor en mantenimiento.";
            default: return string.IsNullOrEmpty(detail) ? $"Error {status}. Intenta de nuevo." : detail;
        }
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

    private void OnClickRegresar()
    {
        if (panelAlRegresar) panelAlRegresar.SetActive(true);
        gameObject.SetActive(false);
    }
}

/// Estado de autenticacion
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
