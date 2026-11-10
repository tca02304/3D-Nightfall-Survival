using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ItemAPI : MonoBehaviour
{
    [SerializeField]
    private string apiUrl = "https://localhost:7133/api/Item/getallitem";

    public bool IsLoaded { get; private set; }

    private void Start()
    {
        StartCoroutine(GetAllItems());
    }

    private IEnumerator GetAllItems()
    {
        using UnityWebRequest request = UnityWebRequest.Get(apiUrl);

        yield return request.SendWebRequest();

        if(request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("API Error: " + request.error);
            yield break;
        }

        string json = request.downloadHandler.text;

        ItemResponse response =
            JsonUtility.FromJson<ItemResponse>(json);

        if(response == null)
        {
            Debug.LogError("Không thể chuyển JSON thành ItemResponse");
            yield break;
        }

        if (!response.success)
        {
            Debug.LogError("API trả về thất bại: " + response.message);
            yield break;
        }

        if(response.data == null)
        {
            Debug.LogError("API không trả về danh sách Item");
            yield break;
        }

        if(ItemDatabase.Instance == null)
        {
            Debug.LogError("Không tìm thấy ItemDatabase");
            yield break;
        }

        ItemDatabase.Instance.SetItems(response.data);

        IsLoaded = true;

        Debug.Log("Đã tải thành công: " + response.data.Length + " Items");
    }
}
