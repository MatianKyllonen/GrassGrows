using UnityEngine;

public static class DemoAnim
{
    static bool Has(Animator a, string n)
    {
        if (!a) return false;
        for (int i = 0; i < a.parameterCount; i++)
            if (a.GetParameter(i).name == n) return true;
        return false;
    }
    //shi
    public static void Trigger(Animator a, string n) { if (Has(a, n)) a.SetTrigger(n); }
    public static void ResetTrigger(Animator a, string n) { if (Has(a, n)) a.ResetTrigger(n); }
    public static void Bool(Animator a, string n, bool v) { if (Has(a, n)) a.SetBool(n, v); }
    public static void Float(Animator a, string n, float v) { if (Has(a, n)) a.SetFloat(n, v); }
}