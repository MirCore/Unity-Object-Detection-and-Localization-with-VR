using UnityEditor;
using UnityEditor.Scripting.Python;

public class MenuItem_KalmanFilterPython_Class
{
   [MenuItem("Python Scripts/KalmanFilterPython")]
   public static void KalmanFilterPython()
   {
       PythonRunner.RunFile("Assets/Scripts/KalmanFilterPython.py");
       }
};
