using UnityEngine;
using UnityEngine.UI;
public sealed class PreviewTextSize : MonoBehaviour {
 public RectTransform Parent;public Text Label;public float FontSize;
 public void Apply(){if(!Parent||!Label)return;var rect=(RectTransform)transform;rect.sizeDelta=Parent.rect.size*10;Label.fontSize=Mathf.RoundToInt(FontSize*10);}
 void LateUpdate()=>Apply();
}
