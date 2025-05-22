// AWSManager.cs
using UnityEngine;

public class AWSManager : MonoBehaviour
{
	public static AWSManager Instance;

	[Header("Managers")]
	public AuthManager authManager;
	public UIManager uiManager;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	void Start()
	{
		authManager.Initialize();
		uiManager.Initialize();
	}
}
