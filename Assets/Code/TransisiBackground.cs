using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TransisiBackground : MonoBehaviour
{
    [Header("Background Settings")]
    [SerializeField] private Image backgroundImage;  // tempat menampilkan gambar
    [SerializeField] private Sprite[] images;        // daftar gambar (9 gambar)
    [SerializeField] private float changeInterval = 10f; // waktu ganti (detik)
    [SerializeField] private float fadeDuration = 1f;    // durasi fade in/out

    private int currentIndex = 0;
    private CanvasGroup canvasGroup;

    private void Start()
    {
        // Tambahkan CanvasGroup ke backgroundImage
        canvasGroup = backgroundImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = backgroundImage.gameObject.AddComponent<CanvasGroup>();
        }

        // Set gambar pertama
        backgroundImage.sprite = images[currentIndex];
        canvasGroup.alpha = 1f;

        // Jalankan loop transisi
        StartCoroutine(BackgroundLoop());
    }

    private IEnumerator BackgroundLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(changeInterval);
            yield return StartCoroutine(FadeOut());

            // Ganti gambar
            currentIndex = (currentIndex + 1) % images.Length;
            backgroundImage.sprite = images[currentIndex];

            yield return StartCoroutine(FadeIn());
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}
