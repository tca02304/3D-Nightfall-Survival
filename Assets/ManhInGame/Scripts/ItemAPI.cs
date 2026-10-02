using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ItemAPI : MonoBehaviour
{
    [SerializeField]
    private string apiUrl = "https://localhost:7133/api/Item/getallitem";

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

        Debug.Log(json);

        ItemResponse response =
            JsonUtility.FromJson<ItemResponse>(json);

        if (response.success)
        {
            foreach(ItemData item in response.data)
            {
                Debug.Log(
                    item.id + "-" +
                    item.name + " - MaxStack: " +
                    item.maxStack
                );
            }
        }
    }
}
