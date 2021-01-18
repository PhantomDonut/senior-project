using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

[InitializeOnLoad]
public static class TimeTrackerInitializer
{
    static TimeTrackerInitializer()
    {
    	TimeTracker.Init();
    }
}

public class TimeTracker : ScriptableObject
{
    List<TimeTrackerEntry> pastEntries;

    public TimeTrackerData data;
    private string scriptPath
    { get { return AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)); } }
    public string assetPath
    {
        get
        {
            return System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(scriptPath));
        }
    }
    private string dataPath
    { get { return assetPath + "/tt_data.asset"; } }
    void LoadDataAsset()
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

    public static TimeTracker instance
    {
        get
        {
            if (_instance == null)
            {
                TimeTracker[] editor = Resources.FindObjectsOfTypeAll<TimeTracker>();

                if (editor != null && editor.Length > 0)
                {
                    _instance = editor[0];

                    for (int i = 1; i < editor.Length; i++)
                    {
                        GameObject.DestroyImmediate(editor[i]);
                    }
                }
            }

            return _instance;
        }

        set
        {
            _instance = value;
        }
    }
    private static TimeTracker _instance;

    #region IDLE
    public double inputIdleTimeCounter = 0;

    public double GetIdleTime()
    {
#if UNITY_EDITOR_OSX
		inputIdleTimeCounter = CGEventSourceSecondsSinceLastEventType(
			_CGEventSourceStateID.kCGEventSourceStateCombinedSessionState, 
			_CGEventType.kCGAnyInputEventType);
#else
        LASTINPUTINFO lii = new LASTINPUTINFO();
        lii.cbSize = (uint)Marshal.SizeOf(lii);

        if (GetLastInputInfo(ref lii))
            inputIdleTimeCounter = (double)(((uint)Environment.TickCount - lii.dwTime) / 1000.0);
        else
            inputIdleTimeCounter = 0;
#endif

        return inputIdleTimeCounter;
    }

#if UNITY_EDITOR_OSX

	public enum _CGEventSourceStateID
	{
		kCGEventSourceStatePrivate = -1,
		kCGEventSourceStateCombinedSessionState = 0,
		kCGEventSourceStateHIDSystemState = 1
	};
	public enum _CGEventType 
	{
		kCGAnyInputEventType		= -1,
		kCGEventLeftMouseDown       = 1,
		kCGEventLeftMouseUp         = 2,
		kCGEventRightMouseDown      = 3,
		kCGEventRightMouseUp        = 4,
		kCGEventMouseMoved          = 5,
		kCGEventLeftMouseDragged    = 6,
		kCGEventRightMouseDragged   = 7,
		kCGEventKeyDown             = 10,
		kCGEventKeyUp               = 11,
		kCGEventFlagsChanged        = 12,
		kCGEventScrollWheel         = 22,
		kCGEventTabletPointer       = 23,
		kCGEventTabletProximity     = 24,
		kCGEventOtherMouseDown      = 25,
		kCGEventOtherMouseUp        = 26,
		kCGEventOtherMouseDragged   = 27,
			//        kCGEventTapDisabledByTimeout = 0xFFFFFFFE,
			//        kCGEventTapDisabledByUserInput = 0xFFFFFFFF,
	};

	[DllImport("/System/Library/Frameworks/Quartz.framework/Versions/Current/Quartz")]
	private static extern double CGEventSourceSecondsSinceLastEventType ( _CGEventSourceStateID stateID, _CGEventType eventType);

#else

    [DllImport("user32.dll")]
    static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

        [MarshalAs(UnmanagedType.U4)]
        public UInt32 cbSize;
        [MarshalAs(UnmanagedType.U4)]
        public UInt32 dwTime;
    }

#endif
    #endregion

    public DateTime startTime;

    // Variables
    double lastUpdateTime;
    double lastSerializeTime;
    float sleepTimeThisSession = 0;
    double updateTime = 1;
    double idleTime = 60;
    double saveTime = 60;
    bool timerPaused;
    bool idleLastTick;

	TimeSpan pureElapsedTime {
		get {
			return DateTime.Now - TimeStarted;
		}
	}
	int pureElapsedTimeInSeconds
	{
		get
		{
			return (int)pureElapsedTime.TotalSeconds;
		}
	}
    int elapsedTimeInSeconds
    {
        get
        {
            return (int)ElapsedTime.TotalSeconds;
        }
    }

    void Save ()
    {
        // Write to data.
        if (data.currentEntry.time > elapsedTimeInSeconds)
        {
//            Debug.Log(string.Format("TIME TRACKER something went wrong with idle times. Current: {0} | New: {1} | idle: {2} | elapsed: {3}", data.currentEntry.time, elapsedTimeInSeconds, sleepTimeThisSession, (DateTime.Now - TimeStarted).TotalSeconds));
            return;
        }
        data.UpdateEntry(elapsedTimeInSeconds);
        EditorUtility.SetDirty(data);

//        Debug.Log("Saved at " + elapsedTimeInSeconds);
    }

    void Tick()
    {
		var elapsed = pureElapsedTimeInSeconds;
        var deltaTime = elapsed - lastUpdateTime;

        lastUpdateTime = elapsed;

        // IDLE
        GetIdleTime();

        // If we're idle, don't save
        bool isIdle = inputIdleTimeCounter > idleTime;
        bool returnedFromSleep = deltaTime > idleTime;
        if (isIdle)
        {
            sleepTimeThisSession += (float)(deltaTime);
//            Debug.Log("Idle " + (int)sleepTimeThisSession);

            if (!idleLastTick)
            {
                idleLastTick = true;

                // GO IDLE
                Save();
                lastSerializeTime = elapsed; 
            }
        }
        else if (returnedFromSleep)
        {
            // We were asleep, maybe because we were debugging or were using menus or some other reason.
            // Resume as usual.

			idleLastTick = false;
			sleepTimeThisSession = 0;

			// RESUME FROM IDLE
			elapsed = 0;
			lastUpdateTime = 0;
			lastSerializeTime = 0; 

			// Start anew.
			startTime = DateTime.Now;
			EditorPrefs.SetString("et_startTime", startTime.ToString());

//			Debug.Log("Creating new Time Tracker " + instance.startTime.ToString());

			// Add a fresh new entry.
			data.AddEntry(startTime);

			CachePastTimes2(); 
        }
        // If we're NOT idle, save
        else
        {
            if (idleLastTick)
            {
                idleLastTick = false;
                sleepTimeThisSession = 0;

                // RESUME FROM IDLE
                elapsed = 0;
                lastUpdateTime = 0;
                lastSerializeTime = 0; 

                // Start anew.
                startTime = DateTime.Now;
                EditorPrefs.SetString("et_startTime", startTime.ToString());

//                Debug.Log("Creating new Time Tracker " + instance.startTime.ToString());

                // Add a fresh new entry.
                data.AddEntry(startTime);

                CachePastTimes2();
            }
            // SAVE
            var saveDelta = elapsed - lastSerializeTime;
            if (saveDelta > saveTime)
            {
                Save();
                lastSerializeTime = elapsed;

                if (TimeStarted.Date != DateTime.Today)
                {
                    elapsed = 0;
                    lastUpdateTime = 0;
                    lastSerializeTime = 0;

                    // Start anew.
                    startTime = DateTime.Now;
                    EditorPrefs.SetString("et_startTime", startTime.ToString());

//                    Debug.Log("Creating new Time Tracker " + instance.startTime.ToString());

                    // Add a fresh new entry.
                    data.AddEntry(startTime);

                    CachePastTimes2();
                }
            }
//            Debug.Log("Tick " + elapsedTimeInSeconds);
        }

        SceneView.RepaintAll();
    }

    void Update()
    {
        // Call this every second.
		var elapsed = pureElapsedTimeInSeconds;
        var deltaTime = elapsed - lastUpdateTime;

        if (deltaTime < updateTime)
            return;

        Tick();
    }
    TimeSpan ElapsedTime
	{ get { return (pureElapsedTime - TimeSpan.FromSeconds(sleepTimeThisSession)); } }
    public static string ToDp(float num, int dp = 2)
    {
        return num.ToString("n" + dp);
    }

    bool IsSameDay(DateTime date, DateTime day)
    {
        return day.Date == date.Date;
    }

    bool IsSameWeek(DateTime date, DateTime week)
    {
        return date.Ticks > StartOfWeek(week).Ticks
            && date.Ticks < StartOfWeek(week).AddDays(7).Ticks;
    }

    bool IsSameMonth(DateTime date, DateTime month)
    {
        return date.Month == month.Month && date.Year == month.Year;
    }

    bool IsSameYear(DateTime date, DateTime year)
    {
        return date.Year == year.Year;
    }

    public string FormatTimeSpan(TimeSpan t)
    {
        string answer = string.Format("{0:D4}h:{1:D2}m:{2:D2}s",//:{3:D3}", 
            Mathf.FloorToInt((float)t.TotalHours),          // 0
            t.Minutes,              // 1
            t.Seconds,              // 2
            t.Milliseconds);        // 3

        return answer;
    }

    public DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek = DayOfWeek.Monday)
    {
        int diff = dt.DayOfWeek - startOfWeek;
        if (diff < 0)
        {
            diff += 7;
        }

        return dt.AddDays(-1 * diff).Date;
    }

    public DateTime StartOfMonth(DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, 1);
    }

    public DateTime StartOfYear(DateTime dt)
    {
        return new DateTime(dt.Year, 1, 1);
    }
    TimeSpan GetTimeSpanFromSessions(List<TimeTrackerEntry> list)
    {
        // Add all session durations up to get the total duration.
        float totalDuration = list.Sum(t => t.time);

        // Return the timespan.
        return TimeSpan.FromSeconds(totalDuration);
    }

    TimeSpan GetTimeFromDay(DateTime date)
    {
        date = date.Date;

        // Get all sessions from the given day.
        var list = entries.Where(t => IsSameDay(t.TimeStarted, date)).ToList();

        return GetTimeSpanFromSessions(list);
    }

    TimeSpan GetTimeFromWeek(DateTime date)
    {
        date = StartOfWeek(date);

        // Get all sessions from the given week.
        var list = entries.Where(t => IsSameWeek(t.TimeStarted, date)).ToList();

        return GetTimeSpanFromSessions(list);
    }

    TimeSpan GetTimeFromMonth(DateTime date)
    {
        date = StartOfMonth(date);

        var list = entries.Where(t => IsSameMonth(t.TimeStarted, date)).ToList();

        return GetTimeSpanFromSessions(list);
    }

    TimeSpan GetTimeFromYear(DateTime date)
    {
        date = StartOfYear(date);

        var list = entries.Where(t => IsSameYear(t.TimeStarted, date)).ToList();

        return GetTimeSpanFromSessions(list);
    }

    TimeSpan GetTotalTime()
    {
        var list = entries;

        return GetTimeSpanFromSessions(entries);
    }
    List<TimeSpan> pastDays;
    List<TimeSpan> pastWeeks;
    List<TimeSpan> pastMonths;
    List<TimeSpan> pastYears;
    TimeSpan pastTotal;

    void CachePastTimes2()
    {
        pastDays = new List<TimeSpan>();
        for (int days = 0; days < 7; days++)
        {
            pastDays.Add(GetTimeFromDay(TimeStarted.AddDays(-days)));
        }

        pastWeeks = new List<TimeSpan>();
        for (int weeks = 0; weeks < 4; weeks++)
        {
            pastWeeks.Add(GetTimeFromWeek(TimeStarted.AddDays(-7 * weeks)));
        }

        pastMonths = new List<TimeSpan>();
        for (int months = 0; months < 3; months++)
        {
            pastMonths.Add(GetTimeFromMonth(TimeStarted.AddMonths(-months)));
        }

        pastYears = new List<TimeSpan>();
        for (int years = 0; years < 3; years++)
        {
            pastYears.Add(GetTimeFromYear(TimeStarted.AddYears(-years)));
        }

        pastTotal = GetTotalTime();



		EditorPrefs.SetInt ("et_pastDay", (int)pastDays[0].TotalSeconds);
		EditorPrefs.SetInt ("et_pastWeek", (int)pastWeeks[0].TotalSeconds);
		EditorPrefs.SetInt ("et_pastMonth", (int)pastMonths[0].TotalSeconds);
		EditorPrefs.SetInt ("et_pastYear", (int)pastYears[0].TotalSeconds);
		EditorPrefs.SetInt ("et_pastTotal", (int)pastTotal.TotalSeconds);
    }

    bool foldedOut
    { get { return data.settings.show; } set { data.settings.show = value; } }
    bool takingABreak;

    List<TimeTrackerEntry> entries
    { get { return data.entries; } }
    DateTime TimeStarted
    { get { return data.currentEntry.TimeStarted; } }
    public string ElapsedTimeString
    { get { return FormatTimeSpan(ElapsedTime); } }

    public void OnSceneGUI (SceneView sceneView)
    {
        // Do your drawing here using Handles.
        Handles.BeginGUI();
        // Do your drawing here using GUI.

//		EditorGUI.TextArea(new Rect(100,100,100,100), "ZOMG TEXT GUI AREA AHHH");

        float width = sceneView.camera.pixelWidth;
        float height = sceneView.camera.pixelHeight;

        float x = 20f;
		#if UNITY_EDITOR_OSX
		float y = sceneView.camera.pixelHeight/2f - 120f;
		#else
		float y = sceneView.camera.pixelHeight - 120f;
		#endif
        float w = 400f;
        float h = 120f;
        float lh = 16f;

//        Rect rf = new Rect(5, y - 32, w, h);
//        Rect r = new Rect(x, y, w, h);
//        Rect rl = new Rect(x, y - 32, w, h);
        Rect rll = new Rect(x, y - lh*2, w, h);
        Rect foldRect = new Rect(5, y, lh, lh);

        foldedOut = EditorGUI.Foldout(foldRect, foldedOut, "");

        /*
        // TAKE A BREAK
        var resumeTime = data.settings.restLength - (elapsedTimeInSeconds / 60f) % data.settings.restPeriod;
        var resumeTimeString = FormatTimeSpan(TimeSpan.FromMinutes(resumeTime));
        if ((EditorApplication.timeSinceStartup / 60f) > data.settings.restPeriod && (EditorApplication.timeSinceStartup / 60f) % data.settings.restPeriod < data.settings.restLength)
        {
            GUI.Label(rl, "!!!TAKE A BREAK!!! Resume in: " + resumeTimeString, EditorStyles.whiteBoldLabel);

            if (!takingABreak)
                EditorApplication.Beep();

            takingABreak = true;
        }
        else
            takingABreak = false;
        */

        // COUNTDOWN
        var deadline = data.settings.deadlineDate;
        var cdTime = deadline - DateTime.Now;
//        var cdTimeString = FormatTimeSpan(cdTime);
        var workingDaysLeft = (float)cdTime.TotalDays;
        string workingDaysLeftString = string.Format("({0} days / {1} weeks left)",
            ToDp(workingDaysLeft, 1), ToDp(workingDaysLeft / 7f, 1));
        string countdownString = deadline.ToString(data.settings.dateFormat) + " - " + workingDaysLeftString;

		if (foldedOut && data.settings.displayDeadline)
            GUI.Label(rll, "DEADLINE: " + countdownString, EditorStyles.whiteLabel);

        // CURRENT
        string current = "NOW:\t" + ElapsedTimeString;

        string idle = "IDLE:\t" + FormatTimeSpan(TimeSpan.FromSeconds(sleepTimeThisSession));
        current += "\t" + idle;

//        TimeSpan theTime = new TimeSpan();
        
        // TODAY
//        string todayString = "TODAY:\t" + FormatTimeSpan(pastDays[0] + ElapsedTime);
//
//        // YESTERDAY
//        string yesterdayString = "YESTERDAY: " + FormatTimeSpan(pastDays[1]);
//
//        // 2 DAYS AGO
//        string twoDaysString = "2 DAYS AGO: " + FormatTimeSpan(pastDays[2]);
//
//        // 3 DAYS AGO
//        string threeDaysString = "3 DAYS AGO: " + FormatTimeSpan(pastDays[3]);
//
//        string daysString = todayString + "\t" + yesterdayString;
//
//        // NOTE: Uncomment these two lines if you wish to display past days.
//        //		daysString += "\t" + twoDaysString;
//        //		daysString += "\t" + threeDaysString;
//
//
//        // WEEK
//        string week0 = "WEEK:\t" + FormatTimeSpan(pastWeeks[0] + ElapsedTime);
//

		// TODAY IN DAYS
		float todayInDays = (float)((pastDays[0] + ElapsedTime).TotalHours) / data.settings.hoursPerDay;
		string todayDays = "";

		if (data.settings.displayDays)
			todayDays += string.Format("{0} days",
				ToDp (todayInDays, 1));

		if (data.settings.displayDays && data.settings.displaySalary)
			todayDays += " - ";
		
		if (data.settings.displaySalary)
			todayDays += string.Format ("${0}",
				ToDp (todayInDays * data.settings.salaryPerDay, 0));
        
		if (data.settings.displayDays || data.settings.displaySalary)
			todayDays = todayDays.Insert (0, "(") + ")";

		// WEEK IN DAYS
        float weekInDays = (float)((pastWeeks[0] + ElapsedTime).TotalHours) / data.settings.hoursPerDay;
		string weekDays = "";

		if (data.settings.displayDays)
			weekDays += string.Format ("{0} days", 
				ToDp (weekInDays, 1));

		if (data.settings.displayDays && data.settings.displaySalary)
			weekDays += " - ";
		
		if (data.settings.displaySalary)
			weekDays += string.Format ("${0}",
				ToDp (weekInDays * data.settings.salaryPerDay, 0));

		if (data.settings.displayDays || data.settings.displaySalary)
			weekDays = weekDays.Insert (0, "(") + ")";
		
//        // LAST WEEK
//        string week1 = "LAST WEEK: " + FormatTimeSpan(pastWeeks[1]);
//
//        // 2 WEEKS AGO
//        string week2 = "2 WEEKS AGO: " + FormatTimeSpan(pastWeeks[2]);
//
//        // 3 WEEKS AGO
//        string week3 = "3 WEEKS AGO: " + FormatTimeSpan(pastWeeks[3]);
//
//        string weeks = week0 + "\t" + week1;
//        // NOTE: Uncomment these two lines if you wish to display past weeks.
//        //		weeks += "\t" + week2;
//        //		weeks += "\t" + week3;
//
//        // TOTAL
//        string total = "TOTAL:\t" + FormatTimeSpan(pastTotal + ElapsedTime);
//
//        // TOTAL IN DAYS
        float totalInDays = (float)((pastTotal + ElapsedTime).TotalHours) / data.settings.hoursPerDay;
		string totalDays = "";

		if (data.settings.displayDays)
			totalDays += string.Format ("{0} days",
				ToDp (totalInDays, 1));

		if (data.settings.displayDays && data.settings.displaySalary)
			totalDays += " - ";

		if (data.settings.displaySalary)
			totalDays += string.Format ("${0}", 
				ToDp (totalInDays * data.settings.salaryPerDay, 0));

		if (data.settings.displayDays || data.settings.displaySalary)
			totalDays = totalDays.Insert (0, "(") + ")";

//
//        string totalTotal = total + "\t" + totalDays;
//
//        // FINAL LABEL
//        string finishedLabel = current + "\n" + daysString + "\n" + weeks + "\n" + totalTotal;

        //if (foldedOut)
        //GUI.Label(r, finishedLabel, EditorStyles.whiteLabel);

        if (foldedOut)
        {
            // TODAY
            GUI.Label(new Rect(x, y, w, lh), "TODAY", EditorStyles.whiteLabel);
			GUIStyle mainTimeStyle = EditorStyles.whiteBoldLabel;
            mainTimeStyle.fontSize = 24;
			mainTimeStyle.normal.textColor = Color.white; 
            GUI.Label(new Rect(x, y + lh, w, lh * 2), FormatTimeSpan(pastDays[0] + ElapsedTime), mainTimeStyle);
//			if (data.settings.displaySalary)
			GUI.Label(new Rect(x, y + lh * 3, w, lh * 3), todayDays, EditorStyles.whiteLabel);

            // THIS WEEK
			if (data.settings.displayThisWeek)
            	GUI.Label(new Rect(x, y + lh * 3 + 20, w, lh * 3), "THIS WEEK\n" + FormatTimeSpan(pastWeeks[0] + ElapsedTime) + "\n" + weekDays, EditorStyles.whiteLabel);

            // TOTAL
			if (data.settings.displayTotal)
				GUI.Label(new Rect(x + (data.settings.displayThisWeek ? 120 : 0), y + lh * 3 + 20, w, lh * 3), "TOTAL\n" + FormatTimeSpan(pastTotal + ElapsedTime) + "\n" + totalDays, EditorStyles.whiteLabel);
        }

        Handles.EndGUI();
    }

    public void Initialize()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        EditorApplication.update -= Update;
        SceneView.onSceneGUIDelegate += OnSceneGUI;
        EditorApplication.update += Update;

        LoadDataAsset();

		if (data.settings.showSettingsOnStartup)
			TimeTrackerSettingsWindow.ShowEditor ();

        // Set start time.
        if (EditorPrefs.HasKey("et_startTime"))
        {
            // Resume after a re-compile.
            startTime = DateTime.Parse(EditorPrefs.GetString("et_startTime", DateTime.Now.ToString()));
			pastDays = new List<TimeSpan>() { TimeSpan.FromSeconds ( EditorPrefs.GetInt ("et_pastDay", 0)) };
			pastWeeks = new List<TimeSpan>() { TimeSpan.FromSeconds ( EditorPrefs.GetInt ("et_pastWeek", 0)) };
			pastMonths = new List<TimeSpan>() { TimeSpan.FromSeconds ( EditorPrefs.GetInt ("et_pastMonth", 0)) };
			pastYears = new List<TimeSpan>() { TimeSpan.FromSeconds ( EditorPrefs.GetInt ("et_pastYear", 0)) };
			pastTotal = TimeSpan.FromSeconds (EditorPrefs.GetInt ("et_pastTotal", 0));

//            Debug.Log("Found existing Time Tracker " + instance.startTime.ToString());
        }
        else
        {
            // Start anew.
            startTime = DateTime.Now;
            EditorPrefs.SetString("et_startTime", startTime.ToString());

//            Debug.Log("Creating new Time Tracker " + instance.startTime.ToString());

            // Add a fresh new entry.
            data.AddEntry(startTime);

			CachePastTimes2();
        }

    }

    void OnDestroy()
    {
        Save();
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        EditorApplication.update -= Update;
    }

//    [MenuItem("Tools/TimeTracker/Show")]
    public static void Init()
    {
        if (instance == null)
        {

            instance = ScriptableObject.CreateInstance<TimeTracker>();
            instance.hideFlags = HideFlags.DontSave;

            EditorPrefs.DeleteKey("et_startTime");

//            Debug.Log("Creating new Time Tracker");
        }
        else
        {
//            Debug.Log("Found existing Time Tracker.");
        }

        EditorApplication.delayCall += instance.Initialize;

        SceneView.RepaintAll();
    }

//    [MenuItem("Tools/TimeTracker/Hide", true)]
    public static bool VerifyCloseAll()
    { 
        return instance != null || Resources.FindObjectsOfTypeAll<TimeTracker>().Length > 0;
    }

//    [MenuItem("Tools/TimeTracker/Hide")]
    public static void CloseAll()
    {
        foreach (TimeTracker editor in Resources.FindObjectsOfTypeAll<TimeTracker>())
            editor.Close();
    }

    public void Close()
    {
        GameObject.DestroyImmediate(this);
    }
}