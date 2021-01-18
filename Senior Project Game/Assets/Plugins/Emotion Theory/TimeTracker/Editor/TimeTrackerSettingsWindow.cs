using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.Globalization;

public class TimeTrackerSettingsWindow : EditorWindow
{
    private TimeTrackerData data;
    private string scriptPath
    { get { return AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)); } }
    public string assetPath
    {
        get
        {
            return System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(scriptPath));
        }
    }

    private string imagePath
    { get { return assetPath + "/Images/tt_settings_banner.png"; } }
    private string dataPath
    { get { return assetPath + "/tt_data.asset"; } }

    private Texture2D _image;
    private Texture2D image
    {
        get
        {
            if (_image == null)
				#if UNITY_5
                _image = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
				#else
				_image = (Texture2D)AssetDatabase.LoadAssetAtPath(imagePath, typeof(Texture2D));
				#endif
            return _image;
        }
    }

	string[] dateFormatOptions = new string[] 
	{ 
		"d\\M\\yyyy",
		"M\\d\\yyyy",
		"yyyy\\M\\d" 
	};
	string dateFormatOption
	{
		get
		{
			return dateFormat.Replace("/","\\");
		}
		set
		{
			dateFormat = value.Replace("\\","/");
		}
	}



	string dateFormat
	{ 
		get { return this.data.settings.dateFormat; }
		set { this.data.settings.dateFormat = value; }
	}

	string dateFormatString
	{ get { return dateFormat.Replace("M","MM").Replace("d","dd"); } }

	public DateTime deadline {
		get {
			return data.settings.deadlineDate;
		}
		set {
			data.settings.deadline = value.ToString ();
		}
	}
	public string deadlineString
	{
		get
		{
			return deadline.ToString (dateFormatString);
		}
		set
		{
			DateTime d = new DateTime();
			if (DateTime.TryParseExact (value, dateFormatString,
				    CultureInfo.CurrentCulture, DateTimeStyles.None, out d))
				deadline = d;
		}
	}

	float hoursPerDay
	{
		get { return data.settings.hoursPerDay; }
		set { data.settings.hoursPerDay = value; }
	}

	// Your hourly wage for optional cost calculation.
	float salaryPerHour
	{
		get { return data.settings.salaryPerHour; }
		set { data.settings.salaryPerHour = value; }
	}

    [MenuItem("Tools/TimeTracker/Settings")]
    public static void ShowEditor()
    {
//        TimeTrackerSettingsWindow editor = EditorWindow.GetWindow<TimeTrackerSettingsWindow>();
		EditorWindow.GetWindow<TimeTrackerSettingsWindow>();
    }

    private void OnEnable()
    {
		titleContent = new GUIContent("Settings");
        minSize = new Vector2(360f, 660f);
        maxSize = new Vector2(360f, 660f);

        Init();
    }
    void LoadDataAsset ()
    {
        // if data doesn't exist, create one.
        if (data == null)
        {
            //Debug.Log("no asset file found, need to reload");
            data = AssetDatabase.LoadAssetAtPath(dataPath, typeof(TimeTrackerData)) as TimeTrackerData;
            if (data == null)
            {
                //Debug.Log("no asset file found, could not reload");	
                data = ScriptableObject.CreateInstance(typeof(TimeTrackerData)) as TimeTrackerData;
                //System.IO.Directory.CreateDirectory(Application.dataPath + _listDataDirectory);
                AssetDatabase.CreateAsset(data, dataPath);
                GUI.changed = true;
            }
        }
    }
    void Init()
    {
        LoadDataAsset();
		data.settings.showSettingsOnStartup = false;
    }
    void Save ()
    {
        // Write to the scriptable object asset.

    }
    void SaveAndClose ()
    {
        Save();
        Close();
    }

    void OnGUI()
    {
		if (!data)
			return;
		
        EditorGUILayout.BeginVertical();
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(image); // 360 x 300
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            string lbl = "Time Tracker by EMOTION THEORY" +
                "\nA tool to help you track your development time in Unity." +
                "\n\nYou can find your time tracking information in the bottom left corner " +
                "of the scene view." +
                "\nIf you ever want to change these settings again, you can open this window " +
                "by going to \"Tools/TimeTracker/Settings\" in the top menu bar.";
            EditorStyles.label.wordWrap = true;
            EditorGUILayout.LabelField(lbl);

            EditorGUILayout.Space();

            
            dateFormatOption = dateFormatOptions[
                EditorGUILayout.Popup("Date Format",
                    Array.IndexOf(dateFormatOptions, dateFormatOption),
                    dateFormatOptions)
            ];
            
			EditorGUILayout.BeginHorizontal ();
			data.settings.displayDeadline = EditorGUILayout.Toggle (data.settings.displayDeadline, GUILayout.Width(16));
			deadlineString = EditorGUILayout.TextField("Deadline", deadlineString);
			EditorGUILayout.EndHorizontal ();

//            restPeriod = EditorGUILayout.FloatField("Take a break every (mins)", restPeriod);
//            restLength = EditorGUILayout.FloatField("Break length (mins)", restLength);
			EditorGUILayout.BeginHorizontal ();
			data.settings.displaySalary = EditorGUILayout.Toggle (data.settings.displaySalary, GUILayout.Width(16));
			salaryPerHour = EditorGUILayout.FloatField("Salary per hour", salaryPerHour);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			data.settings.displayDays = EditorGUILayout.Toggle (data.settings.displayDays, GUILayout.Width(16));
			hoursPerDay = EditorGUILayout.FloatField("Work hours per day", hoursPerDay);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			data.settings.displayThisWeek = EditorGUILayout.Toggle ("Show current week hours", data.settings.displayThisWeek);
			data.settings.displayTotal = EditorGUILayout.Toggle ("Show total hours", data.settings.displayTotal);
			EditorGUILayout.EndHorizontal ();


            if (GUILayout.Button("Save & Close"))
                SaveAndClose();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayoutOption glo = GUILayout.Width(80);

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Twitter", glo))
                    Application.OpenURL("http://www.twitter.com/emotiontheory");
                EditorGUILayout.Space();
                if (GUILayout.Button("Facebook", glo))
                    Application.OpenURL("http://www.facebook.com/emotiontheory");
                EditorGUILayout.Space();
                if (GUILayout.Button("YouTube", glo))
                    Application.OpenURL("http://www.youtube.com/emotiontheory");

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Forum", glo))
                    Application.OpenURL("http://forum.unity3d.com/threads/time-tracker-seamless-non-intrusive-tracking-of-your-dev-time.395145/");
                EditorGUILayout.Space();
                if (GUILayout.Button("Website", glo))
                    Application.OpenURL("http://www.emotiontheory.com");
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Email: adam@emotiontheory.com"))
                    Application.OpenURL("mailto:adam@emotiontheory.com");
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("A Unity Asset by EMOTION THEORY.");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

    }
}