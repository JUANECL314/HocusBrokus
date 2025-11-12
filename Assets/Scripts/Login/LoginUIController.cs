using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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

    [Header("Photon / Net")]
    [SerializeField] private NetworkManager photonConnector; // arrástralo en el Inspector

    [Header("API")]
    [SerializeField] private string apiBaseUrl = "https://hokusbackend-production.up.railway.app";
    [SerializeField] private float requestTimeoutSeconds = 15f;

    [Header("Opcional")]
    [SerializeField] private bool rememberToken = true;
    [SerializeField] private bool autoConnectIfRememberedToken = false;
    [SerializeField] private bool connectPhotonAfterLogin = true;

    // --- DTOs ---
    [Serializable] private class LoginPayload { public string email; public string password; }
    [Serializable] private class UserDTO { public int id; public string username; public string email; }
    [Serializable] private class LoginResponse { public string token; public UserDTO user; }

    // Errores FastAPI
    [Serializable] private class DetailList { public List<DetailItem> detail; }
    [Serializable] private class DetailItem { public string msg; public string type; public List<string> loc; }
    [Serializable] private class DetailString { public string detail; }

    private void Awake()
    {
        if (btnIngresar) btnIngresar.onClick.AddListener(OnClickIngresar);
        if (btnRegresar) btnRegresar.onClick.AddListener(OnClickRegresar);
        if (lblEstado) lblEstado.text = "";

        // Autologin opcional
        if (rememberToken && autoConnectIfRememberedToken && AuthState.TryLoadFromPrefs())
        {
            SetEstado($"Bienvenido de nuevo, {AuthState.Username}.");

            // Asegura nickname y conexión
            if (connectPhotonAfterLogin)
            {
                SetPhotonNick(AuthState.Username, AuthState.Email);
                if (photonConnector != null) photonConnector.ConectarServidor();
            }

            // Ir directo al Lobby local si así lo quieres
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.EntrarLobbyIndividual();
            else
                SceneManager.LoadScene("Lobby");
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
            SetEstado("Escribe tu correo y contraseña.");
            return;
        }

        // ⚠️ Importante: ya NO cambiamos de escena aquí.
        // Solo iniciamos el login contra el backend.
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
        bool transportError = req.result != UnityWebRequest.Result.Success;
#else
        bool transportError = req.isNetworkError || req.isHttpError;
#endif
        var status = (int)req.responseCode;
        var body = req.downloadHandler.text;

        if (transportError || status < 200 || status >= 300)
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
            SetEstado("No se recibió token. Intenta de nuevo.");
            ToggleInteractable(true);
            yield break;
        }

        // Guarda token + user para la tienda y endpoints protegidos
        AuthState.SetToken(resp.token, resp.user?.id ?? 0, resp.user?.username, resp.user?.email, rememberToken);

        // Establece NickName y conecta Photon si procede
        if (connectPhotonAfterLogin)
        {
            SetPhotonNick(resp.user?.username, resp.user?.email);
            if (photonConnector != null) photonConnector.ConectarServidor();
        }

        // ✅ Ahora sí: cambia de escena al Lobby (local)
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.EntrarLobbyIndividual();
        else
            SceneManager.LoadScene("Lobby");

        SetEstado("¡Login correcto!");
        ToggleInteractable(true);
    }

    private static void SetPhotonNick(string username, string email)
    {
        string nick = !string.IsNullOrWhiteSpace(username)
            ? username
            : (!string.IsNullOrWhiteSpace(email) ? email.Split('@')[0] : "Player");
        PhotonNetwork.NickName = nick;
        Debug.Log("[Login] NickName = " + PhotonNetwork.NickName);
    }

    private string MapFriendlyError(int status, string body)
    {
        string detail = null;
        try
        {
            var s = JsonUtility.FromJson<DetailString>(body);
            if (s != null && !string.IsNullOrEmpty(s.detail)) detail = s.detail;
        }
        catch { }

        if (string.IsNullOrEmpty(detail))
        {
            try
            {
                var l = JsonUtility.FromJson<DetailList>(body);
                if (l != null && l.detail != null && l.detail.Count > 0)
                    detail = l.detail[0].msg;
            }
            catch { }
        }

        var d = (detail ?? body ?? "").ToLowerInvariant();

        if (d.Contains("password") || d.Contains("user") || d.Contains("email") ||
            d.Contains("not found") || status == 401 || status == 403)
            return "Credenciales incorrectas.";
        if (status == 429 || d.Contains("too many"))
            return "Demasiados intentos, espera un momento.";
        if (status == 422)
            return "Datos inválidos.";
        if (status == 0)
            return "Sin conexión a internet.";
        if (status >= 500)
            return "Error del servidor. Intenta más tarde.";
        return "Error al iniciar sesión, intenta de nuevo.";
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

// ---- AuthState compartido con la Tienda ----
public static class AuthState
{
    public static string Token { get; private set; }
    public static int UserId { get; private set; }
    public static string Username { get; private set; }
    public static string Email { get; private set; }

    private const string PP_TOKEN = "auth.token";
    private const string PP_USER = "auth.user";
    private const string PP_USERID = "auth.userId";

    public static void SetToken(string token, int userId, string username, string email, bool persist)
    {
        Token = token;
        UserId = userId;
        Username = username;
        Email = email;

        if (persist)
        {
            PlayerPrefs.SetString(PP_TOKEN, token);
            PlayerPrefs.SetInt(PP_USERID, userId);
            PlayerPrefs.SetString(PP_USER, username ?? "");
            PlayerPrefs.Save();
        }
    }

    public static bool TryLoadFromPrefs()
    {
        if (!PlayerPrefs.HasKey(PP_TOKEN)) return false;
        Token = PlayerPrefs.GetString(PP_TOKEN, "");
        UserId = PlayerPrefs.GetInt(PP_USERID, 0);
        Username = PlayerPrefs.GetString(PP_USER, "");
        return !string.IsNullOrEmpty(Token) && UserId > 0;
    }

    public static void Clear()
    {
        Token = null;
        UserId = 0;
        Username = null;
        Email = null;
        PlayerPrefs.DeleteKey(PP_TOKEN);
        PlayerPrefs.DeleteKey(PP_USER);
        PlayerPrefs.DeleteKey(PP_USERID);
    }
}
