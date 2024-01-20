using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Candidate
{
    public string Name;
    public string Description;
    public JOB_DEPARTMENT m_Department;

    public static Dictionary<JOB_DEPARTMENT, string> DEPARTMENT_TEXT = new Dictionary<JOB_DEPARTMENT, string>()
    {
        { JOB_DEPARTMENT.HR, "Hr" },
        { JOB_DEPARTMENT.FINANCE, "Finance" },
        { JOB_DEPARTMENT.MARKETING, "Marketing" },
        { JOB_DEPARTMENT.PRODUCTION, "Production" },
        { JOB_DEPARTMENT.RESEARCH, "Research" },
        { JOB_DEPARTMENT.QA, "QA" },
        { JOB_DEPARTMENT.SECURITY, "Security" }
    };
}

