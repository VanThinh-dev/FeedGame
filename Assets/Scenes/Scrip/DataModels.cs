using System;
using System.Collections.Generic;

[Serializable]
public class VocabWord
{
    public string id;
    public string english;
    public string vietnamese;
    public string lessonId;
    public string lessonName;
}

[Serializable]
public class LessonData
{
    public string id;
    public string name;
    public int    wordCount;
    public string createdAt;
    public bool   isCompleted;
}