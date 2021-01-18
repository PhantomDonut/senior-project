using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;

public class TimeTrackerData : ScriptableObject
{
//	public const string VERSION = "1.0";

    public static bool IsEmptyOrNull(ICollection list)
    {
        if (list == null || list.Count == 0)
            return true;
        return false;
    }

    //[HideInInspector]
    public List<TimeTrackerEntry> entries = new List<TimeTrackerEntry>();
    public TimeTrackerSettings settings = new TimeTrackerSettings();

    #region PROPERTIES
    public TimeTrackerEntry currentEntry
    {
        get
        {
            if (IsEmptyOrNull(entries))
                return null;
            int lastIndex = entries.Count - 1;
            return entries[lastIndex];
        }
    }  

    public Dictionary<DateTime, List<TimeTrackerEntry>> entriesByDay
    {
        get
        {
            return new Dictionary<DateTime, List<TimeTrackerEntry>>();
        }
    }
    public Dictionary<DateTime, List<TimeTrackerEntry>> entriesByWeek
    {
        get
        {
            return new Dictionary<DateTime, List<TimeTrackerEntry>>();
        }
    }
    public Dictionary<DateTime, List<TimeTrackerEntry>> entriesByMonth
    {
        get
        {
            return new Dictionary<DateTime, List<TimeTrackerEntry>>();
        }
    }
    public Dictionary<DateTime, List<TimeTrackerEntry>> entriesByYear
    {
        get
        {
            return new Dictionary<DateTime, List<TimeTrackerEntry>>();
        }
    }
    #endregion

    public TimeTrackerData()
    {

    }

    public void AddEntry(DateTime timeBegan)
    {
        TimeTrackerEntry entry = new TimeTrackerEntry(timeBegan, 0);
        if (entries == null)
            entries = new List<TimeTrackerEntry>();
        entries.Add(entry);
    }

    public void UpdateEntry (int time)
    {
        if (currentEntry != null)
            currentEntry.time = time;
    }
    
}

[Serializable]
public class TimeTrackerSettings
{
    public bool showSettingsOnStartup = true;
    public bool show = true;
//    public string dateFormat = "d/M/yyyy";
	private const string ET_TT_DATEFORMAT = "ET_TT_DATEFORMAT";
	public string dateFormat
	{ 
		get { return EditorPrefs.GetString (ET_TT_DATEFORMAT, "d/M/yyyy"); }
		set { EditorPrefs.SetString (ET_TT_DATEFORMAT, value); }
	}
    public string deadline;
    public float restPeriod = 60;
    public float restLength = 2;
    public float hoursPerDay = 8;
    public float salaryPerHour = 25;
    public DateTime deadlineDate
    { get { return string.IsNullOrEmpty(deadline) ? new DateTime() : DateTime.Parse(deadline); } }
    public float salaryPerDay
    { get { return salaryPerHour * hoursPerDay; } }

	public bool displayDeadline;
	public bool displaySalary;
	public bool displayDays;
	public bool displayThisWeek = true;
	public bool displayTotal = true;

	public TimeTrackerSettings ()
	{
		deadline = DateTime.Today.AddMonths (1).Date.ToString ();
	}
}

[Serializable]
public class TimeTrackerEntry
{
    public DateTime TimeStarted
    { get { return new DateTime().AddSeconds(timeBegan); } }

    public string timeBeganDate;
    public double timeBegan;
    public int time;

    public TimeTrackerEntry(DateTime timeBegan, int time)
    {
        var dt = new DateTime();
//        var ts = timeBegan - dt;
        this.timeBegan = Math.Round( (timeBegan - dt).TotalSeconds );
        this.timeBeganDate = TimeStarted.ToString();
        this.time = time;
    }
}
