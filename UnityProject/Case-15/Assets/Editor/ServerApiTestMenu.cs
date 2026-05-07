using UnityEditor;
using UnityEngine;

public class ServerApiTestMenu
{
    [MenuItem("Core15/Test Server Connection")]
    public static async void TestConnection()
    {
        Debug.Log("Suncuya ping atılıyor...");
        string pingResult = await BackendConnector.PingServerAsync();
        
        if (!string.IsNullOrEmpty(pingResult))
        {
            Debug.Log($"Ping Başarılı: {pingResult}");

            Debug.Log("POST isteği gönderiliyor...");
            string jsonPayload = "{\"message\": \"Unity'den selamlar!\"}";
            string postResult = await BackendConnector.PostJsonAsync("/test_connection", jsonPayload);
            
            if (!string.IsNullOrEmpty(postResult))
            {
                Debug.Log($"POST Başarılı: {postResult}");
            }
        }
        else
        {
            Debug.LogError("Sunucuya ulaşılamadı. Python sunucusunun çalıştığından emin olun.");
        }
    }
}
