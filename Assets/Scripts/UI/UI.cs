using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UIElements;

public class ConnectionUI : MonoBehaviour
{
    private TextField ipPortField;

    private Button btnSingleplayer;
    private Button btnMultiplayer;
    private Button btnHost;
    private Button btnBackMain;
    private Button btnBackMP;
    private Button btnConnect;
    private Button btnConnectGo;

    private VisualElement rootUI;
    private VisualElement containerMainMenu;
    private VisualElement containerMP;
    private VisualElement containerConnection;

    private void OnEnable()
    {
        rootUI = GetComponent<UIDocument>().rootVisualElement;

        #region main menu
            containerMainMenu = rootUI.Q<VisualElement>("containerMainMenu");

            btnSingleplayer = rootUI.Q<Button>("btnSingleplayer");
            btnMultiplayer = rootUI.Q<Button>("btnMultiplayer");

            btnSingleplayer.clicked += OnSingleplayerClicked;
            btnMultiplayer.clicked += OnMultiplayerClicked;
        #endregion

        #region MP
            containerMP = rootUI.Q<VisualElement>("containerMP");

            btnHost = rootUI.Q<Button>("btnHost");
            btnConnect = rootUI.Q<Button>("btnConnect");
            btnBackMain = rootUI.Q<Button>("btnBackMain");

            btnHost.clicked += OnHostClicked;
            btnConnect.clicked += OnConnectClicked;
            btnBackMain.clicked += OnBackMainClicked;
        #endregion

        #region connect
            containerConnection = rootUI.Q<VisualElement>("containerConnection");

            ipPortField = rootUI.Q<TextField>("ipPortField");
            btnConnectGo = rootUI.Q<Button>("btnConnectGo");
            btnBackMP = rootUI.Q<Button>("btnBackMP");

            btnConnectGo.clicked += OnConnectGoClicked;
            btnBackMP.clicked += OnBackMPClicked;
        #endregion
    }

    private void OnSingleplayerClicked()
    {
        ConfigureLocalTransport();
        if (NetworkManager.Singleton.StartHost())
            rootUI.style.display = DisplayStyle.None;
    }

    private void OnMultiplayerClicked()
    {
        containerMainMenu.style.display = DisplayStyle.None;
        containerMP.style.display = DisplayStyle.Flex;
        containerConnection.style.display = DisplayStyle.None;
    }

    private void OnConnectClicked()
    {
        containerMainMenu.style.display = DisplayStyle.None;
        containerMP.style.display = DisplayStyle.None;
        containerConnection.style.display = DisplayStyle.Flex;
    }

    private void OnBackMPClicked()
    {
        containerMainMenu.style.display = DisplayStyle.None;
        containerMP.style.display = DisplayStyle.Flex;
        containerConnection.style.display = DisplayStyle.None;
    }

    private void OnBackMainClicked()
    {
        containerMainMenu.style.display = DisplayStyle.Flex;
        containerMP.style.display = DisplayStyle.None;
        containerConnection.style.display = DisplayStyle.None;
    }

    private void OnHostClicked()
    {
        ConfigureOnlineTransport();
        if (NetworkManager.Singleton.StartHost())
            rootUI.style.display = DisplayStyle.None;
    }

    private void OnConnectGoClicked()
    {
        var ipPort = ipPortField.value.Split(':');
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipPort[0], ushort.Parse(ipPort[1]));
        if (NetworkManager.Singleton.StartClient())
            rootUI.style.display = DisplayStyle.None;
    }

    private void ConfigureLocalTransport()
    {
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

        // Use localhost and a fixed port
        transport.ConnectionData.Address = "0.0.0.0";
        transport.ConnectionData.Port = 7777;
    }

    private void ConfigureOnlineTransport()
    {
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

        // Use localhost and a fixed port
        transport.ConnectionData.Address = "127.0.0.1";
        transport.ConnectionData.Port = 7777;
    }
}