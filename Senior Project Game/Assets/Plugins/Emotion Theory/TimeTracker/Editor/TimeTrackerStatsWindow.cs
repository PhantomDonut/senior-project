using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

public class TimeTrackerStatsWindow : EditorWindow
{
	private bool initialized = false;

	public string GetUsername ()
	{
		#if UNITY_EDITOR_OSX
		return Environment.UserName;
		#else 
		return Environment.UserName;
		#endif
	}

	List<string> GetLabelsForSelected ()
	{
		var l = new List<string>();

		var prevTabId = tabId;

		// week
		int lblCount = 0;

		if (tabId == StatsTab.Week)
		{ 
			lblCount = 7; 
			tabId = StatsTab.Day; 
			selectedDate = StartOfWeek(selectedDate);
		}
		else if (tabId == StatsTab.Month)
		{
			lblCount = DateTime.DaysInMonth( selectedDate.Year, selectedDate.Month );
			tabId = StatsTab.Day;
			selectedDate = StartOfMonth(selectedDate);
		}
		else if (tabId == StatsTab.Year)
		{
			lblCount = 12;
			tabId = StatsTab.Month;
			selectedDate = StartOfYear(selectedDate);
		}
		else if (tabId == StatsTab.AllTime)
		{
			tabId = StatsTab.Month;
			lblCount = sliderCount;
			sliderVal = 0;
		}

		for (int i = 0; i < lblCount; i++)
		{
			l.Add(DateLabelFromSlider(unclampedSliderVal + i));
		}
			
		tabId = prevTabId;

		return l;
	}

	List<float> GetResultsForSelected ()
	{
		var l = new List<float>();

		var prevTabId = tabId;

		// week
		int resCount = 0;

		if (tabId == StatsTab.Week)
		{ 
			resCount = 7; 
			tabId = StatsTab.Day; 
			selectedDate = StartOfWeek(selectedDate);
		}
		else if (tabId == StatsTab.Month)
		{
			resCount = DateTime.DaysInMonth( selectedDate.Year, selectedDate.Month );
			tabId = StatsTab.Day;
			selectedDate = StartOfMonth(selectedDate);
		}
		else if (tabId == StatsTab.Year)
		{
			resCount = 12;
			tabId = StatsTab.Month;
			selectedDate = StartOfYear(selectedDate);
		}
		else if (tabId == StatsTab.AllTime)
		{
			tabId = StatsTab.Month;
			resCount = sliderCount;
			sliderVal = 0;
		}

		for (int i = 0; i < resCount; i++)
		{
			l.Add((float)ResultFromSlider(unclampedSliderVal + i).TotalHours);
		}

		tabId = prevTabId;

		return l;
	}

	static public string SaveOpenDialog ( string defaultName )
	{
		return UnityEditor.EditorUtility.SaveFilePanel
			( "Export to CSV", "", defaultName + ".csv", "csv" );
	}

	static public void SaveCSV( string report, string path )
	{
		var sr = System.IO.File.CreateText(path);
		sr.WriteLine( report );
		sr.Close();
	}

	public string GenerateReport ()
	{
		string report = "";

		#if UNITY_5
		report += Application.productName;
		report += "\n";
		report += Application.companyName;
		report += "\n";
		#else
		report += PlayerSettings.productName;
		report += "\n";
		report += PlayerSettings.companyName;
		report += "\n";
		#endif
		report += GetUsername ();
		report += "\n";
		report += "\n";
		report += reportLabel;
		report += "\n";
		report += dateLabel;
		report += "\n";
		report += "\n";

		List<string> labels = GetLabelsForSelected ();
		List<float> results = GetResultsForSelected ();

		for (int i = 0; i < labels.Count; i++)
			report += labels[i] + (i < labels.Count - 1 ? "," : "");
		report += "\n";
		for (int i = 0; i < results.Count; i++)
			report += ToDp(results[i],1).Replace(",","") + (i < results.Count - 1 ? "," : "");
		report += "\n";

		report += "\n";

		report += string.Format("Salary Per Hour,Total Hours,Salary ({0})", tabLabel);
		report += "\n";

		string sal = ToDp(data.settings.salaryPerHour, 2).Replace(",","");
		string tHrs = ToDp(results.Sum(), 1).Replace(",","");
		string tSal = ToDp(data.settings.salaryPerHour * results.Sum(), 2).Replace(",","");

		report += string.Format("${0},{1},${2}", sal, tHrs, tSal);

		return report;
	}

	public void ExportToCSV ()
	{
		string report = GenerateReport ();

		string path = SaveOpenDialog ( PlayerSettings.productName + "_" + dateLabel.Replace ('/','-').Replace (' ', '_') );

		if ( ! string.IsNullOrEmpty( path ) )
			SaveCSV( report, path );
	}

	public string dateFormatString
	{ get { return data.settings.dateFormat; } }


//    [MenuItem("Tools/TimeTracker/Reset")]
    public static void TestReset()
    {
        TimeTracker.instance.data.entries = new List<TimeTrackerEntry>();
        TimeTracker.CloseAll();
        EditorApplication.delayCall += TimeTracker.Init;
    }
//    [MenuItem("Tools/TimeTracker/Test Data")]
    public static void TestData()
    {
        List<TimeTrackerEntry> list = new List<TimeTrackerEntry>();

        DateTime prevDate = new DateTime(2014, 03, 13);
        for (int i = 0; i < 260 * 2; i++)
        {
            int next = 1;
            if (prevDate.DayOfWeek == DayOfWeek.Friday)
                next = 3;
            prevDate = prevDate.AddDays(next);
            list.Add(new TimeTrackerEntry(prevDate, UnityEngine.Random.Range(0, 10 * 3600)));
        }

		list = list.GetRange (list.Count - 100, 90);

        TimeTracker.instance.data.entries = list;
    }

    [MenuItem("Tools/TimeTracker/Statistics")]
	public static void ShowEditor ()
	{
//		TimeTrackerStatsWindow editor = EditorWindow.GetWindow<TimeTrackerStatsWindow> ();
		EditorWindow.GetWindow<TimeTrackerStatsWindow> ();
	}

	private void OnEnable()
	{
		titleContent = new GUIContent("Statistics");
		minSize = new Vector2(640f, 480f);

		Init();
	}

	int sliderCount
	{
		get
		{
			if (tabId == StatsTab.Day)
				return days.Count;
			else if (tabId == StatsTab.Week)
				return weeks.Count;
			else if (tabId == StatsTab.Month)
				return months.Count;
			else if (tabId == StatsTab.Year)
				return years.Count;

			return 0;
		}
	}

	int unclampedSliderVal
	{
		get
		{
			if (tabId == StatsTab.AllTime)
				return 0;

			var date = earliest.Date;
			var cur = selectedDate.Date;
			int diff = 0;

			if (tabId == StatsTab.Day)
			{
				diff = (int) (cur - date).TotalDays;
			}
			if (tabId == StatsTab.Week)
			{
				date = StartOfWeek(date);
				cur = StartOfWeek(cur);

				diff = (int) ((cur - date).TotalDays / 7);
			}
			else if (tabId == StatsTab.Month)
			{
				date = StartOfMonth(date);
				cur = StartOfMonth(cur);

				diff = (int)((cur.Year - date.Year) * 12) + cur.Month - date.Month;
			}
			else if (tabId == StatsTab.Year)
			{
				date = StartOfYear(date);
				cur = StartOfYear(cur);

				diff = cur.Year - date.Year;
			}

			int val = diff;

			return val;
		}
	}

	int sliderVal
	{ 
		get
		{
			if (tabId == StatsTab.AllTime)
				return 0;
				
			var date = earliest.Date;
			var cur = selectedDate.Date;
			int diff = 0;

			if (tabId == StatsTab.Day)
			{
				diff = (int) (cur - date).TotalDays;
			}
			if (tabId == StatsTab.Week)
			{
				date = StartOfWeek(date);
				cur = StartOfWeek(cur);

				diff = (int) ((cur - date).TotalDays / 7);
			}
			else if (tabId == StatsTab.Month)
			{
				date = StartOfMonth(date);
				cur = StartOfMonth(cur);

				diff = (int)((cur.Year - date.Year) * 12) + cur.Month - date.Month;
			}
			else if (tabId == StatsTab.Year)
			{
				date = StartOfYear(date);
				cur = StartOfYear(cur);

				diff = cur.Year - date.Year;
			}

			int val = Mathf.Clamp( diff, 0, sliderCount );

			return val;
		}
		set
		{
			int val = Mathf.Clamp( value, 0, sliderCount );

			selectedDate = GetDateFromSlider(val);
		}
	}

	DateTime GetDateFromSlider(int value)
	{
		int val = value;

		if (tabId == StatsTab.Day)
			return earliest.Date.AddDays(val);
		else if (tabId == StatsTab.Week)
			return StartOfWeek(earliest).AddDays(7 * val);
		else if (tabId == StatsTab.Month)
			return StartOfMonth(earliest).AddMonths(val);
		else if (tabId == StatsTab.Year)
			return StartOfYear(earliest).AddYears(val);

		return earliest.Date.AddDays(val);
	}

	string DateLabelFromSlider ( int value )
	{
		return DateLabelFromDate( GetDateFromSlider ( value ) );
	}

	string masterDateLabel
	{
		get {
			var cost = "$" + ToDp ((float)result.TotalHours * data.settings.salaryPerHour);
			return string.Format ("{0}\n{1} - {2}", dateLabel, resultText, cost);
		}
	}

	string DateLabelFromDate ( DateTime dt )
	{
		string s = "All Time";

		if (tabId == StatsTab.Day)
			s = string.Format("{0} {1}", 
				dt.ToString("dddd"),
				dt.ToString(dateFormatString));
		else if (tabId == StatsTab.Week)
			s = string.Format("Week {0} - {1}", 
				StartOfWeek(dt).ToString(dateFormatString),
				StartOfWeek(dt).AddDays(6).ToString(dateFormatString));
		else if (tabId == StatsTab.Month)
			s = string.Format("{0} {1}",
				dt.ToString("MMMM"),
				dt.Year);
		else if (tabId == StatsTab.Year)
			s = string.Format("{0}", 
				dt.Year);


//		s += string.Format ("@ ${3}/hr", data.settings.salaryPerHour);

		return s;
	}

	string reportLabel
	{
		get
		{
			string lbl = "";

			if (tabId == StatsTab.Day)
				lbl = "DAILY";
			else if (tabId == StatsTab.Week)
				lbl = "WEEKLY";
			else if (tabId == StatsTab.Month)
				lbl = "MONTHLY";
			else if (tabId == StatsTab.Year)
				lbl = "ANNUAL";

			return string.Format("{0} {1}", lbl, "REPORT");
		}
	}

	string dateLabel
	{
		get
		{
			return DateLabelFromDate( selectedDate );
		}
	}

	string dateString
	{
		get
		{
			return selectedDate.Date.ToString(dateFormatString);
		}
		set
		{
			var date = DateTime.ParseExact( 
				value, 
				dateFormatString, 
				CultureInfo.CurrentCulture ).Date;

			if (tabId == StatsTab.Day)
				selectedDate = date;
			else if (tabId == StatsTab.Week)
				selectedDate = StartOfWeek(date);
			else if (tabId == StatsTab.Month)
				selectedDate = StartOfMonth(date);
			else if (tabId == StatsTab.Year)
				selectedDate = StartOfYear(date);
		}
	}

	DateTime selectedDate;

	TimeSpan ResultFromSlider ( int value )
	{
		DateTime dt = GetDateFromSlider(value);

		return ResultFromDate(dt);
	}

	TimeSpan ResultFromDate (DateTime dt)
	{
		TimeSpan r = new TimeSpan();

		if (tabId == StatsTab.Day)
			days.TryGetValue(dt, out r);
		else if (tabId == StatsTab.Week)
			weeks.TryGetValue(dt, out r);
		else if (tabId == StatsTab.Month)
			months.TryGetValue(dt, out r);
		else if (tabId == StatsTab.Year)
			years.TryGetValue(dt, out r);
		else
			r = total;

		return r;
	}

	string ResultTextFromTimeSpan (TimeSpan r)
	{
		if (r.TotalHours == 0)
			return "No recorded time.";

		return ToDp( (float)r.TotalHours, 1) + "h";
	}

	string ResultTextFromFloat (float f)
	{
		if (f <= 0)
			return "0.0h";

		return ToDp( f, 1) + "h";
	}

	string resultText
	{
		get
		{
			return ResultTextFromTimeSpan(result);
		}
	}
	TimeSpan result
	{
		get
		{
			return ResultFromDate( selectedDate );
		}
	}

	enum StatsTab
	{
		Day,
		Week,
		Month,
		Year,
		AllTime,
	}

	StatsTab tabId;
	string tabLabel = "";

	public static T Next<T>(T src) where T : struct
	{
		if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argumnent {0} is not an Enum", typeof(T).FullName));

		T[] Arr = (T[])Enum.GetValues(src.GetType());
		int j = Array.IndexOf<T>(Arr, src) + 1;
		return (Arr.Length==j) ? Arr[0] : Arr[j];            
	}
		
	void OnGUI ()
	{
		if (!initialized || !tt || !data)
			return;
		
		if (GUI.Button (new Rect (20, 10, 80, 20), "Refresh"))
			Init ();

		if ( tabId != StatsTab.Day)
			if ( GUI.Button(new Rect(20, 40, 80, 20), "Export") )
				ExportToCSV ();
		
		EditorGUILayout.BeginVertical ();
		{
			EditorGUILayout.Space( );
			EditorGUILayout.BeginHorizontal ();
			{
				GUILayout.FlexibleSpace ( );

				if (GUILayout.Button(tabId.ToString(), GUILayout.Width(80)))
					tabId = Next(tabId);
				
				GUILayout.FlexibleSpace ( );

			} 
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space( );
			EditorGUILayout.BeginHorizontal ();
			{
				
//				if ( GUILayout.Button ("<") )
//					sliderVal--;


				
				GUILayout.FlexibleSpace ( );

				EditorGUILayout.BeginVertical ();
				{
					// Label
					GUIStyle lblStyle = EditorStyles.boldLabel;
					lblStyle.alignment = TextAnchor.MiddleCenter;
					EditorGUILayout.SelectableLabel(masterDateLabel, lblStyle);

					if (sliderCount > 1)
					{
						// Slider
						EditorGUILayout.BeginHorizontal();
						{
							GUILayout.FlexibleSpace ( );
							if ( GUILayout.Button ("<") )
								sliderVal--;
							if ( GUILayout.Button (">") )
								sliderVal++;
							sliderVal = EditorGUILayout.IntSlider(sliderVal, 0, sliderCount-1,
								GUILayout.Width(200));
							GUILayout.FlexibleSpace ( );
						}
						EditorGUILayout.EndHorizontal();
					}
					else
					{
						sliderVal = 0;
					}

					// disp buttons
					EditorGUILayout.Space( );
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.FlexibleSpace();
						string spreadLabel = "▮";
						for (int i = 1; i <= spread; i++)
							spreadLabel += "▮▮";
						if (GUILayout.Button(spreadLabel, GUILayout.Width(80)))
						{
							spread++;
							if (spread > 3)
								spread = 0;
						}
						GUILayout.FlexibleSpace();
					}
					EditorGUILayout.EndHorizontal();

				}
				EditorGUILayout.EndVertical ();

				GUILayout.FlexibleSpace ( );

//				if ( GUILayout.Button (">") )
//					sliderVal++;
			}
			EditorGUILayout.EndHorizontal ();


	
			// DISPLAY

			float hrs = (float)result.TotalHours;

			float max = 0;

			float best = 0;
			string bestDate = "";
			float avg = 0;
			int count = 0;
			tabLabel = "";

			if (tabId == StatsTab.Day)
			{
				max = (float)daysBestResult.TotalHours;
				best = max;
				bestDate = DateLabelFromDate( daysBestDate );
				avg = daysAvg;
				count = daysCount;
				tabLabel = "day";
			}
			else if (tabId == StatsTab.Week)
			{
				max = (float)weeksBestResult.TotalHours;
				best = max;
				bestDate = DateLabelFromDate( weeksBestDate );
				avg = weeksAvg;
				count = weeksCount;
				tabLabel = "week";
			}
			else if (tabId == StatsTab.Month)
			{	
				max = (float)monthsBestResult.TotalHours;
				best = max;
				bestDate = DateLabelFromDate( monthsBestDate );
				avg = monthsAvg;
				count = monthsCount;
				tabLabel = "month";
			}
			else if (tabId == StatsTab.Year)
			{
				max = (float)yearsBestResult.TotalHours;
				best = max;
				bestDate = DateLabelFromDate( yearsBestDate );
				avg = yearsAvg;
				count = yearsCount;
				tabLabel = "year";
			}
			else if (tabId == StatsTab.AllTime)
			{
				max = hrs;
				tabLabel = "all time";
			}

			// Goal
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				EditorGUILayout.BeginVertical();
				{
					EditorGUILayout.Space ();
					// Total Goal.
					GUIStyle lblStyle = EditorStyles.boldLabel;
					lblStyle.alignment = TextAnchor.UpperCenter;

	//				EditorGUILayout.LabelField(lbl, lblStyle, GUILayout.Height(80), GUILayout.Width(300));

					if (tabId != StatsTab.AllTime)
					{
						EditorGUILayout.LabelField(
							"Best: ", 
							string.Format("{0}h on {1}", ToDp(best, 1), bestDate), GUILayout.Width(400));

						EditorGUILayout.LabelField(
							"Average: ", 
							string.Format("{0}h / {1}", ToDp(avg, 1), tabLabel));

						EditorGUILayout.LabelField(
							"Total: ", 
							string.Format("{0}h across {1} working {2}{3}", ToDp((float)total.TotalHours, 1), count.ToString(), tabLabel, count > 1 ? "s" : ""));
						
//						EditorGUILayout.LabelField(
//							"No. of " + tabLabel + "s: ", 
//							count.ToString());
					}
					else
						EditorGUILayout.LabelField(
							"Total time: ", 
							ToDp(max, 1) + "h", GUILayout.Width(400));

				}
				EditorGUILayout.EndVertical();
				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal ();

			// Background
			GUIDrawRect( bgRect, new Color(0.95f,0.95f,0.95f,1f) );
			// Center / selection
			GUIDrawRect( new Rect((position.width/2f)-(w), 200, w*2, h), new Color(0.9f,0.9f,0.9f,1f) );
			// Today / current
			if (TodayIsVisible)
				GUIDrawRect( new Rect( (position.width/2f)-(w) + (w * 2 * (sliderCount + (tabId == StatsTab.AllTime ? 0 : -1) - sliderVal)), 200, w*2, h), new Color(0.9f,0.9f,0.2f,0.3f) );

			DrawBar(0, hrs, dateLabel, max);

			if (tabId != StatsTab.AllTime)
			{
				for (int i = 1; i <= spread; i++)
				{
					DrawBar(w * -2 * i, (float)ResultFromSlider(sliderVal-i).TotalHours, DateLabelFromSlider(sliderVal-i), max);
					DrawBar(w * 2 * i, (float)ResultFromSlider(sliderVal+i).TotalHours, DateLabelFromSlider(sliderVal+i), max);
				}
			}

		}
		EditorGUILayout.EndVertical ();

	}

	public bool TodayIsVisible
	{ 
		get
		{
			return sliderVal >= sliderCount - spread && sliderVal <= sliderCount + spread;
		}
	}


	Rect bgRect
	{ get { return new Rect(0, top, position.width, h); } }

	Rect barsRect
	{ get { return new Rect(margin, top, position.width - margin2, h); } }

	int w
	{ get { return (int) ( (position.width - margin2) / (1f + spread * 4f) ); } }
	int h
	{ get { return (int)position.height - bottom - top; } }
	int barHeight { get { return (h - margin); } }
	int o = 2;
	int margin = 20;
	int margin2 { get { return margin * 2; } }
	int bottom = 80;
	int top = 200;

	void DrawBar (int wOffset, float hrs, string dt, float max)
	{
        // Bar
        float perc = 0;
        if (max > 0)
            perc = Mathf.Clamp01(hrs/max);

		Rect barRect1 = new Rect(wOffset + (position.width/2f)-(w/2f)-o, position.height - bottom, w+(2*o), -perc * barHeight - o);
		GUIDrawRect ( barRect1, Color.black );

		Rect barRect = new Rect(wOffset + (position.width/2f)-(w/2f), position.height - bottom, w, -perc * barHeight);
		GUIDrawRect ( barRect, Color.green );

		// Label
		Rect lblRect = new Rect(barRect.x, barRect.y + barRect.height-20, barRect.width, 20);
		GUI.Label( lblRect, ResultTextFromFloat(hrs), EditorStyles.largeLabel );

		// Date label
		GUIStyle dtStyle = EditorStyles.largeLabel;
		dtStyle.alignment = TextAnchor.UpperCenter;
		dtStyle.wordWrap = true;
		Rect dtRect = new Rect(barRect.x - 20, barRect.y + 10, barRect.width + 40, 60);
		GUI.Label( dtRect, dt, dtStyle );
	}

	int spread = 3;

	private static Texture2D _staticRectTexture;
	private static GUIStyle _staticRectStyle;

	public static void InitBox (Color color)
	{
		if( _staticRectTexture == null )
		{
			_staticRectTexture = new Texture2D( 1, 1 );
		}

		if( _staticRectStyle == null )
		{
			_staticRectStyle = new GUIStyle();
		}

		_staticRectTexture.SetPixel( 0, 0, color );
		_staticRectTexture.Apply();

		_staticRectStyle.normal.background = _staticRectTexture;
	}

	// Note that this function is only meant to be called from OnGUI() functions.
	public static void GUIDrawRect( Rect position, Color color )
	{
		InitBox(color);

		GUI.Box( position, GUIContent.none, _staticRectStyle );
	}

	void Init ()
	{
		if (!tt || !data) {
			EditorApplication.delayCall += Init;
			return;
		}

        CachePastTimes();

		this.sliderVal = sliderCount - 1;

		initialized = true;
	}

    #region TIME TRACKER VARIABLES

    public TimeTracker tt
	{ get { return TimeTracker.instance; } }

    public TimeTrackerData data
    {  get { return tt.data; } }

    // CACHED



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
        return GetTimeSpanFromSessions(entries);
    }

    void CachePastTimes()
    {
        entries = data.entries;

        earliest = entries.Min(x => x.TimeStarted);
        today = entries.Max(x => x.TimeStarted);
        List<TimeSpan> tList = new List<TimeSpan>();

        // DAYS
        days = new Dictionary<DateTime, TimeSpan>();
        var date = today.Date;
        var dMax = earliest.Date;
        while (date >= dMax)
        {
            days.Add(date, GetTimeFromDay(date));

            date = date.AddDays(-1);
        }


        tList = days.Values.Where(x => x.TotalHours > 0).ToList();
        if (tList.Count == 0)
            tList = days.Values.ToList();

        daysCount = tList.Count;
        daysAvg = (float)tList.Average(x => x.TotalHours);
        daysBestDate = days.Single(x => x.Value.TotalHours == tList.Max(y => y.TotalHours)).Key;
        daysBestResult = days[daysBestDate];
        


        // WEEKS
        weeks = new Dictionary<DateTime, TimeSpan>();
        date = StartOfWeek(today);
        dMax = StartOfWeek(earliest);
        while (date >= dMax)
        {
            weeks.Add(date, GetTimeFromWeek(date));

            date = date.AddDays(-7);
        }


        tList = weeks.Values.Where(x => x.TotalHours > 0).ToList();

        if (tList.Count == 0)
            tList = weeks.Values.ToList();

        weeksCount = tList.Count;
        weeksAvg = tList.Count > 0 ? (float)tList.Average(x => x.TotalHours) : 0;
        weeksBestDate = weeks.Single(x => x.Value.TotalHours == tList.Max(y => y.TotalHours)).Key;
        weeksBestResult = weeks[weeksBestDate];


        // MONTHS
        months = new Dictionary<DateTime, TimeSpan>();
        date = StartOfMonth(today);
        dMax = StartOfMonth(earliest);
        while (date >= dMax)
        {
            months.Add(date, GetTimeFromMonth(date));

            date = date.AddMonths(-1);
        }


        tList = months.Values.Where(x => x.TotalHours > 0).ToList();
        if (tList.Count == 0)
            tList = months.Values.ToList();
        monthsCount = tList.Count;
        monthsAvg = tList.Count > 0 ? (float)tList.Average(x => x.TotalHours) : 0;
        monthsBestDate = months.Single(x => x.Value.TotalHours == tList.Max(y => y.TotalHours)).Key;
        monthsBestResult = months[monthsBestDate];
        


        // YEARS
        years = new Dictionary<DateTime, TimeSpan>();
        date = StartOfYear(today);
        dMax = StartOfYear(earliest);
        while (date >= dMax)
        {
            years.Add(date, GetTimeFromYear(date));

            date = date.AddYears(-1);
        }


        tList = years.Values.Where(x => x.TotalHours > 0).ToList();
        if (tList.Count == 0)
            tList = years.Values.ToList();
        yearsCount = tList.Count;
        yearsAvg = tList.Count > 0 ? (float)tList.Average(x => x.TotalHours) : 0;
        yearsBestDate = years.Single(x => x.Value.TotalHours == tList.Max(y => y.TotalHours)).Key;
        yearsBestResult = years[yearsBestDate];
        

        tList.Clear();
        tList = null;

        // ALL TIME
        total = GetTotalTime();
    }

    List<TimeTrackerEntry> entries;

    Dictionary<DateTime, TimeSpan> days;
    Dictionary<DateTime, TimeSpan> weeks;
    Dictionary<DateTime, TimeSpan> months;
    Dictionary<DateTime, TimeSpan> years;
    TimeSpan total;

    public DateTime earliest;
    public DateTime today;

    TimeSpan daysBestResult;
    DateTime daysBestDate;
    float daysAvg;
    int daysCount;

    TimeSpan weeksBestResult;
    DateTime weeksBestDate;
    float weeksAvg;
    int weeksCount;

    TimeSpan monthsBestResult;
    DateTime monthsBestDate;
    float monthsAvg;
    int monthsCount;

    TimeSpan yearsBestResult;
    DateTime yearsBestDate;
    float yearsAvg;
    int yearsCount;

    #endregion

    #region DateTime

    public static string ToDp(float num, int dp = 2)
	{
		return num.ToString("n" + dp);
	}

	bool IsSameDay ( DateTime date, DateTime day )
	{
		return day.Date == date.Date;
	}

	bool IsSameWeek ( DateTime date, DateTime week )
	{
		return date.Ticks > StartOfWeek(week).Ticks
			&& date.Ticks < StartOfWeek(week).AddDays(7).Ticks;
	}

	bool IsSameMonth ( DateTime date, DateTime month )
	{
		return date.Month == month.Month && date.Year == month.Year;
	}

	bool IsSameYear ( DateTime date, DateTime year )
	{
		return date.Year == year.Year;
	}

	public string FormatTimeSpan (TimeSpan t)
	{
		string answer = string.Format("{0:D4}h:{1:D2}m:{2:D2}s",//:{3:D3}", 
			Mathf.FloorToInt((float)t.TotalHours), 			// 0
			t.Minutes, 				// 1
			t.Seconds, 				// 2
			t.Milliseconds);		// 3

		return answer;
	}

	public DateTime StartOfWeek( DateTime dt, DayOfWeek startOfWeek = DayOfWeek.Monday)
	{
		int diff = dt.DayOfWeek - startOfWeek;
		if (diff < 0)
		{
			diff += 7;
		}

		return dt.AddDays(-1 * diff).Date;
	}

	public DateTime StartOfMonth( DateTime dt )
	{
		return new DateTime( dt.Year, dt.Month, 1 );
	}
		
	public DateTime StartOfYear( DateTime dt )
	{
		return new DateTime( dt.Year, 1, 1 );
	}

	#endregion


}