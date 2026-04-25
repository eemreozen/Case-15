using UnityEngine;
using UnityEngine.Events;

public class ComputerInteract : MonoBehaviour
{
    [Header("Arayuz (UI)")]
    public GameObject computerUI;
    public KeyCode interactKey = KeyCode.E;

    [Header("Multiplayer & Baglantilar Icin")]
    public UnityEvent onComputerOpen;  // Oyuncu hareketini kitlemek icin
    public UnityEvent onComputerClose; // Oyuncu hareketini acmak icin

    private bool isPlayerNearby = false;
    private bool isUIOpen = false;

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(interactKey))
        {
            ToggleComputerUI();
        }
    }

    public void ToggleComputerUI()
    {
        isUIOpen = !isUIOpen;
        computerUI.SetActive(isUIOpen);

        if (isUIOpen) onComputerOpen.Invoke();
        else onComputerClose.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ileride buraya: if(other.GetComponent<NetworkIdentity>().isLocalPlayer) eklenecek
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            Debug.Log("SSSSSSSSSSSSSSS");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (isUIOpen) ToggleComputerUI(); // Uzaklasirsa UI otomatik kapansin
        }
    }
}
