using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

public class MapScript : MonoBehaviour
{
    
    private float lat,lon;
    public int zoom;
    public int mapWidth, mapHeight;
    public enum mapType {roadmap, satellite, hybrid, terrain};
    public mapType mapSelected;
    public int scale;
    public RawImage mapTile;
    public Text info;
    public GameObject loader;
    private bool loading, loadingMap;
    private string url="";
    private string mapKey = "AIzaSyD81WNH98vRB360tc2NVM09hQBYVFAwgfk";

    // Start is called before the first frame update
    void Start()
    {
        //Requesting permission for location at runtime
		#if PLATFORM_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
                Permission.RequestUserPermission(Permission.CoarseLocation);
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                Permission.RequestUserPermission(Permission.FineLocation);
		#endif
        StartCoroutine(GetLocationData());
        loader.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.location.isEnabledByUser && Input.location.status != LocationServiceStatus.Failed && Input.location.status != LocationServiceStatus.Initializing){
			// print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.timestamp);
			lat = float.Parse(Input.location.lastData.latitude.ToString());
			lon = float.Parse(Input.location.lastData.longitude.ToString());
            if(!loadingMap)
                StartCoroutine(GetGoogleMap(lat, lon));
            if(!loading)
                StartCoroutine(GetLocationAddress(lat, lon));
        }
    }

    IEnumerator GetGoogleMap(float lat, float lon)
    {
        loadingMap = true;
        url = "https://maps.googleapis.com/maps/api/staticmap?center="+ lat + "," + lon +
              "&zoom=" + zoom + "&size=" + mapWidth + "x" + mapHeight + "&maptype=" + mapSelected +
              "&markers=color:red%7Clabel:G%7C"+ lat + "," + lon + "&sensor=false&key=" + mapKey;
        // WWW www = new WWW(url);
        // yield return www;
        // mapTile.texture = www.texture;
        // loading = false;
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.LogError(string.Format("Error: {0}",request.error));
        }
        else
        {
            // Response can be accessed through: request.downloadHandler.text
            Debug.Log(request.downloadHandler.text);
            mapTile.texture = DownloadHandlerTexture.GetContent(request);
        }
        
        loader.SetActive(false);
        loadingMap = false;
    }

	IEnumerator GetLocationData()
	{
		// First, check if user has location service enabled
		if (!Input.location.isEnabledByUser){
			info.text = "Locaiton service is disabled by the user";
			Debug.Log("Locaiton service is disabled by the user");
//			yield break;
		}

		// Start service before querying location
		Input.location.Start(0.1f,0.1f);

		// Wait until service initializes
		int maxWait = 20;
		while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
		{
			yield return new WaitForSeconds(1);
			maxWait--;
		}

		// Service didn't initialize in 20 seconds
		if (maxWait < 1)
		{
			info.text = "Timed out";
			print("Timed out");
//			yield break;
		}

		// Connection has failed
		if (Input.location.status == LocationServiceStatus.Failed)
		{
			info.text = "Unable to determine device location";
			print("Unable to determine device location");
//			yield break;
		}
		else
		{
			StartCoroutine(GetLocationData());
		}
	}

    IEnumerator GetLocationAddress(float lat,  float lon) {
        loading = true;
		url = "https://maps.googleapis.com/maps/api/geocode/json?latlng=" + lat + "," + lon + "&key=" + mapKey;
		UnityWebRequest www = UnityWebRequest.Get(url);
		yield return www.Send();
		if(www.isNetworkError) {
			Debug.Log(www.error);
		}
		else {
			string result = www.downloadHandler.text;
            // Debug.Log(result);
			JSONObject obj = new JSONObject(result);
            info.text = lat + "," + lon +"\n" + obj[1][obj[1].Count - 1]["formatted_address"].ToString().Substring(9).Trim('"');
            Debug.Log(""+lat + "," + lon +"\nLocation Address: " + info.text);
		}
        loading = false;
	}
}
