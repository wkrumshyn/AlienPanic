
//Add if using Snazzy Grid
//#define USING_SNAZZY_GRID

//Add if using Auto Grid Snap (System used in Unity Game Maker Project)
#define USING_AUTO_GRID_SNAP

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Linq;

public class AssetPlacementWindow :  EditorWindow {
	public static AssetPlacementWindow instance = null;
	
	static void CreateAssetPlacementSystem () {
		string systemName = "AP.AssetPlacementSystem";
		GameObject systemContainer = null;
		systemContainer = GameObject.Find (systemName);
		if (!systemContainer) {
			systemContainer = new GameObject (systemName);
			
			systemContainer.AddComponent<AssetPlacementChoiceSystem> ();
			if (AssetPlacementChoiceSystem.instance) {
				DestroyImmediate (AssetPlacementChoiceSystem.instance.gameObject);
			}
			
			systemContainer.AddComponent<AssetPlacementPositionSystem> ();
		}
		
		var choiceSystem = systemContainer.GetComponent<AssetPlacementChoiceSystem> ();
		AssetPlacementChoiceSystem.instance = choiceSystem;
		choiceSystem.shouldResetAssets = true;
		choiceSystem.Refresh ();	
	}
	
	[MenuItem( AssetPlacementGlobals.CommandPath + "Open Window" )]
	static void Init() {
		if (!instance) {
			AssetPlacementWindow window = (AssetPlacementWindow)EditorWindow.GetWindow (typeof(AssetPlacementWindow));
			window.title = "AP";
			window.minSize = new Vector2 (200, 100);
			instance = window;
			
			CreateAssetPlacementSystem ();
		}
	}
	
	static void RefreshAutoSnap (GameObject placedAsset) {
		#if USING_SNAZZY_GRID
		SnazzyToolsEditor.SnapPos(true, true, true);
		#endif
		#if USING_AUTO_GRID_SNAP
		if (Utils.GameObjectFunctions.HasMesh(placedAsset)) {
			var snapSize = Utils.GameObjectFunctions.CreateRectFromMeshes(placedAsset);
			
			if (EditorPrefs.GetBool (AssetPlacementGlobals.SnapUpdate)) {
				EditorPrefs.SetFloat (AutoGridSnap.MoveSnapXKey, snapSize.width);
				EditorPrefs.SetFloat (AutoGridSnap.MoveSnapYKey, snapSize.height);
			}
		}
		#endif
	}
	
	static void SetTabContainerParent (GameObject placedAsset) {
		if (AssetPlacementChoiceSystem.instance) {
			if (AssetPlacementChoiceSystem.instance.TabContainerDictionary.ContainsKey (AssetPlacementChoiceSystem.selectedAsset.tab)) {
				var container = AssetPlacementChoiceSystem.instance.TabContainerDictionary [AssetPlacementChoiceSystem.selectedAsset.tab];
				placedAsset.transform.parent = container.transform;
			}
		}
	}
	
	static void DestroyOverlappedAssets (GameObject placedAsset) {
		var otherAssets = Physics2D.OverlapCircleAll (new Vector2 (placedAsset.transform.localPosition.x, placedAsset.transform.localPosition.y), 0.1f);
		var distance = Mathf.Infinity;
		foreach (var asset in otherAssets) {
			if (asset.transform.parent.name == placedAsset.transform.parent.name) {
				if (asset.gameObject != placedAsset) {
					DestroyImmediate (asset.gameObject);
				}
			}
		}
	}
	
	//TODO Allow user to set these hotkeys
	[MenuItem(AssetPlacementGlobals.CommandPath + "Commands/Place Asset &_d")] 
	static void PlaceAsset() {
		if (AssetPlacementChoiceSystem.selectedAsset.gameObject == null) {
			return;
		}
		
		if (AssetPlacementChoiceSystem.instance && AssetPlacementChoiceSystem.selectedAsset != null && AssetPlacementChoiceSystem.selectedAsset.gameObject != null) {
			if(AssetPlacementChoiceSystem.instance.TabContainerDictionary.Count == 0) {
				AssetPlacementChoiceSystem.instance.RefreshTabContainers();
			}
			
			var placedAsset = PrefabUtility.InstantiatePrefab(AssetPlacementChoiceSystem.selectedAsset.gameObject) as GameObject; 	
			placedAsset.transform.localPosition = AssetPlacementPositionSystem.selectedPosition;
			Selection.activeGameObject = placedAsset;
			
			SetTabContainerParent (placedAsset);
			RefreshAutoSnap (placedAsset);
			DestroyOverlappedAssets (placedAsset);
		}
	}	
	
	
	[MenuItem(AssetPlacementGlobals.CommandPath + "Commands/Select &_w")]
	static void SelectPlaceAssetSystem() {
		if (AssetPlacementChoiceSystem.instance) {
			Selection.activeGameObject = AssetPlacementChoiceSystem.instance.gameObject;
		}
	}	
	
	#if USING_AUTO_GRID_SNAP
	[MenuItem("Edit/Commands/ToggleAutoSnapUpdate &_f")]
	static void ToggleAutoSnapUpdate() {
		EditorPrefs.SetBool (AssetPlacementGlobals.SnapUpdate, !EditorPrefs.GetBool (AssetPlacementGlobals.SnapUpdate));
	}	
	#endif
	
	void RefreshSpriteSheetIcons () {
		foreach (var icon in miscIcons) {
			if (icon.name == "LightFull") { 
				lightIcon = Utils.TextureUtils.ConvertSpriteToTexture (icon);
			} else if (icon.name == "AmbientMusic") {
				soundIcon = Utils.TextureUtils.ConvertSpriteToTexture (icon);
			} else if (icon.name == "Trigger1") {
				triggerIcon = Utils.TextureUtils.ConvertSpriteToTexture (icon);
			}
		}
	}
	
	void OnInspectorUpdate() {
		if (background == null) {
			Load();
		}
		
		if (!lightIcon && background) {
			RefreshSpriteSheetIcons ();
		}
	}
	
	private Texture2D background = null;
	private Texture2D backgroundAlpha = null;
	private Texture2D windowTitle = null;
	private Sprite[] miscIcons = null;
	
	private Texture2D lightIcon = null;
	private Texture2D soundIcon = null;
	private Texture2D triggerIcon = null;
	
	
	//TODO Create some kinda of do once function
	bool warnOnce = true;
	
	void Load() {
		if (background) {
			return;
		}
		
		string path = "Assets/"+AssetPlacementGlobals.InstallPath+"AssetPlacement/Resources/GUI/";
		background = AssetDatabase.LoadAssetAtPath(path+"BG.jpg",typeof(Texture2D)) as Texture2D;
		DontDestroyOnLoad (background);
		
		if (!background && warnOnce) {
			warnOnce = false;
			Debug.Log ("AssetPlacement InstallPath Needs Fixing");
		}
		
		backgroundAlpha = AssetDatabase.LoadAssetAtPath(path+"BGAlpha.png",typeof(Texture2D)) as Texture2D;
		DontDestroyOnLoad (backgroundAlpha);
		
		windowTitle = AssetDatabase.LoadAssetAtPath(path+"Title.jpg",typeof(Texture2D)) as Texture2D;
		DontDestroyOnLoad (windowTitle);
		
		miscIcons = AssetDatabase.LoadAllAssetsAtPath(path+"MiscIcons.png").OfType<Sprite>().ToArray(); 
	}
	
	void CreateTitleLogo (float width, ref float distanceFromTop) {
		EditorGUI.LabelField (new Rect (0, 0, width, 64), new GUIContent (windowTitle, "//TODO Add Tooltip"));
		distanceFromTop += 64;
	}
	
	static void CreateAutoSnapToggle (float width, ref float distanceFromTop) {
		#if USING_AUTO_GRID_SNAP
		EditorPrefs.SetBool (
			AssetPlacementGlobals.SnapUpdate,
			EditorGUI.Toggle (
			new Rect (-1, distanceFromTop, width, 20),
			"Update Auto Snap",
			EditorPrefs.GetBool (AssetPlacementGlobals.SnapUpdate, false)));
		
		distanceFromTop += 20;
		#endif
	}
	
	void CreateShowLabelsToggle (float width, ref float distanceFromTop) {
		float toggleHeight = 16;
		var shouldShowLabels = EditorPrefs.GetBool (AssetPlacementGlobals.ShowLabels);
		shouldShowLabels = EditorGUI.Toggle (new Rect (0, distanceFromTop, width, toggleHeight), "Show Labels", shouldShowLabels);
		EditorPrefs.SetBool (AssetPlacementGlobals.ShowLabels, shouldShowLabels);
		
		distanceFromTop += toggleHeight;
	}
	
	void CreateToggleTabSelection (float width, ref float distanceFromTop) {
		float toggleHeight = 16;
		
		var shouldShowAll = EditorPrefs.GetBool (AssetPlacementGlobals.ShowAll);
		shouldShowAll = EditorGUI.Toggle (new Rect (0, distanceFromTop, width, toggleHeight), "Show All", shouldShowAll);
		EditorPrefs.SetBool (AssetPlacementGlobals.ShowAll, shouldShowAll);
		
		distanceFromTop += toggleHeight;
		if (!shouldShowAll || true) {
			float popupHeight = 20;
			int selectedTabNumber = EditorPrefs.GetInt (AssetPlacementGlobals.SelectedTab);
			selectedTabNumber = EditorGUI.Popup (new Rect (0, distanceFromTop, width, popupHeight), selectedTabNumber, AssetPlacementChoiceSystem.instance.tabNames.ToArray ());
			EditorPrefs.SetInt (AssetPlacementGlobals.SelectedTab, selectedTabNumber);
			distanceFromTop += popupHeight;
			
			if (selectedTabNumber < AssetPlacementChoiceSystem.instance.tabList.Count) {
				AssetPlacementChoiceSystem.instance.selectedTab = AssetPlacementChoiceSystem.instance.tabList [selectedTabNumber];
			}
		} else {
			distanceFromTop += 8;
		}
	}
	
	void CreateHotkeyLabel (Rect buttonRect, string keyLabel) {
		Rect labelRect = new Rect (buttonRect.x + buttonRect.width * 0.7f, buttonRect.y + buttonRect.height * 0.7f, buttonRect.width * 0.3f, buttonRect.width * 0.3f);
		
		GUI.DrawTexture (new Rect (labelRect.x - labelRect.width * 0.1f,
		                           labelRect.y - labelRect.height * 0.1f, 
		                           labelRect.width,
		                           labelRect.height)
		                 , backgroundAlpha);
		
		GUIStyle labelStyle = new GUIStyle ();
		labelStyle.fontSize = 18;
		labelStyle.fontStyle = FontStyle.Bold;
		labelStyle.normal.textColor = Color.black;
		GUI.Label (labelRect, keyLabel, labelStyle);
	}
	
	void CreateTabLabel (AssetPlacementData assetData, Rect buttonRect) {
		Rect labelRect = new Rect(buttonRect.x + buttonRect.width * 0.1f, buttonRect.y + buttonRect.height * 0.1f, buttonRect.width * 0.8f, buttonRect.width * 0.2f); 
		
		GUI.DrawTexture (new Rect (labelRect.x,
		                           labelRect.y - labelRect.height * 0.1f, 
		                           labelRect.width,
		                           labelRect.height)
		                 , backgroundAlpha);
		
		var labelStyle = new GUIStyle ();
		labelStyle.fontSize = 14;
		labelStyle.fontStyle = FontStyle.Bold;
		labelStyle.normal.textColor = Color.black;
		GUI.Label (labelRect, assetData.tab, labelStyle);
	}
	
	bool hasMadeAnIconRenderAsset = false;
	void CreateAssetButtons (float width, ref float distanceFromTop) {
		var shouldShowAll = EditorPrefs.GetBool (AssetPlacementGlobals.ShowAll);
		var shouldShowLabels = EditorPrefs.GetBool (AssetPlacementGlobals.ShowLabels);
		
		int assetIndex = 0;
		int index = -1;
		
		float xCoord = 0;
		float yCoord = 0;
		
		foreach (var assetData in AssetPlacementChoiceSystem.instance.assetList) {
			index++;
			
			if (assetData.tab != AssetPlacementChoiceSystem.instance.selectedTab.name && !shouldShowAll) {
				assetIndex++;
				continue;
			}
			
			if(assetData.gameObject == null) { continue; }
			
			var tempObject = GameObject.Instantiate(assetData.gameObject) as GameObject;
			
			Texture2D usedTexture = null;
			if (tempObject.GetComponent<SpriteRenderer> () || 
			    tempObject.GetComponentsInChildren<SpriteRenderer>().Length > 0) {
				
				usedTexture = tempObject.GetComponent<SpriteRenderer> ().sprite.texture;
			} else if (tempObject.GetComponent<MeshFilter> () || 
			           tempObject.GetComponentsInChildren<MeshFilter>().Length > 0) {
				var tempTexture = AssetPlacementIconRenderer.CreateTextureFromCamera(assetData, ref hasMadeAnIconRenderAsset);
				usedTexture = tempTexture; 
			} else if (tempObject.GetComponent<Light> () || 
			           tempObject.GetComponentsInChildren<Light>().Length > 0) {
				usedTexture = lightIcon; 
			} else if (tempObject.GetComponent<AudioSource> () || 
			           tempObject.GetComponentsInChildren<AudioSource>().Length > 0) {
				usedTexture = soundIcon; 
			} else {
				usedTexture = triggerIcon;
			}
			
			DestroyImmediate(tempObject);
			
			var downState = new GUIStyle(GUI.skin.button);
			downState.normal.background = GUI.skin.button.active.background;
			downState.active.background = GUI.skin.button.active.background;
			
			
			var upState =  new GUIStyle(GUI.skin.button);
			var buttonStyle = EditorPrefs.GetInt (AssetPlacementGlobals.SelectedAssetNumber) == index ? downState : upState;
			
			var buttonRect = new Rect ((width / 3.0f) * xCoord, distanceFromTop + (width / 3.0f) * yCoord, (width / 3.0f), (width / 3.0f));
			if (usedTexture && GUI.Button (buttonRect, usedTexture, buttonStyle)) {
				EditorPrefs.SetInt (AssetPlacementGlobals.SelectedAssetNumber, assetIndex);
			}
			
			string keyLabel = assetData.keyCode.ToString();
			if(keyLabel == "None") { keyLabel = "[]"; } 
			else if(keyLabel.Length > 1) { keyLabel = keyLabel.Remove(0,keyLabel.Length - 1); }
			
			if(shouldShowLabels) {
				if(assetData.tab == AssetPlacementChoiceSystem.instance.selectedTab.name) {
					CreateHotkeyLabel (buttonRect, keyLabel);
				}
				if(shouldShowAll) {
					CreateTabLabel (assetData, buttonRect);
				}
			}
			
			assetIndex++; xCoord++;
			if (xCoord > 2) { xCoord = 0; yCoord++; }
		}
		
		if (hasMadeAnIconRenderAsset) {
			AssetPlacementIconRenderer.CleanUpRender3DAssets();
		}
	}
	
	Vector2 scrollPosition = Vector2.zero;
	void CreateAssetListScrollView (float width, ref float distanceFromTop) {
		if (AssetPlacementChoiceSystem.instance.selectedTab == null) {
			return;
		}
		
		float dist = distanceFromTop;
		int assetCount = 0;
		var shouldShowAll = EditorPrefs.GetBool (AssetPlacementGlobals.ShowAll);
		
		foreach (var assetData in AssetPlacementChoiceSystem.instance.assetList) {
			if (assetData.tab != AssetPlacementChoiceSystem.instance.selectedTab.name && !shouldShowAll) {
				continue;
			}
			assetCount++;
		}
		float scrollMax = (((assetCount / 3) + 1.0f) * width / 3.0f) + 2.0f;
		float viewMax = Screen.height - dist - 5;
		float buffer = scrollMax > viewMax ? 12 : 0;
		scrollPosition = GUI.BeginScrollView (new Rect (0.0f, dist, width, viewMax), scrollPosition, new Rect (0.0f, dist, width - buffer, scrollMax));
		
		CreateAssetButtons (width - buffer, ref distanceFromTop);
		GUI.EndScrollView ();
	}
	
	static GUIStyle CreateWarningFontStyle () {
		var angryFont = new GUIStyle ();
		angryFont.normal.textColor = Color.red;
		angryFont.fontStyle = FontStyle.Bold;
		return angryFont;
	}
	
	public void OnGUI() {
		instance = this;
		
		if (AssetPlacementChoiceSystem.instance == null) {
			GUI.Label (new Rect(0, 0, Screen.width, 120), "[Error]\nSystem Was Deleted\nPlease Refresh Window\nTo Keep Using", CreateWarningFontStyle ());
			return;
		}
		
		if (background) {
			float width = Screen.width;
			EditorGUI.DrawPreviewTexture (new Rect (0, 0, Screen.width, Screen.height), background);
			
			float distanceFromTop = 0.0f;
			
			CreateTitleLogo (width, ref distanceFromTop);
			
			if(AssetPlacementChoiceSystem.instance.tabList == null || AssetPlacementChoiceSystem.instance.tabList.Count == 0) {
				GUI.Label (new Rect(0, distanceFromTop, width, 20), "No Assets Found", CreateWarningFontStyle ());
				return;
			}
			
			CreateAutoSnapToggle (width, ref distanceFromTop);
			CreateShowLabelsToggle (width, ref distanceFromTop);
			CreateToggleTabSelection (width, ref distanceFromTop);
			CreateAssetListScrollView (width, ref distanceFromTop);
		}
	}
}