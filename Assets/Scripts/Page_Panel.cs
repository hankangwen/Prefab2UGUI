using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
public class Page_Panel : MonoBehaviour
{
    private Text Txt_Title = null;
	private Image Img_Image = null;
	private Button Btn_Button = null;
	private RawImage Raw_RawImage = null;
	private InputField Input_InputField = null;
	private Slider Slider_Slider = null;
	private Scrollbar Scr_Scrollbar = null;
	
    //auto
    private void Awake()
    {
        Txt_Title = transform.Find("Txt_Title").GetComponent<Text>();
		Img_Image = transform.Find("Img_Image").GetComponent<Image>();
		Btn_Button = transform.Find("Btn_Button").GetComponent<Button>();
		Raw_RawImage = transform.Find("Raw_RawImage").GetComponent<RawImage>();
		Input_InputField = transform.Find("Input_InputField").GetComponent<InputField>();
		Slider_Slider = transform.Find("Slider_Slider").GetComponent<Slider>();
		Scr_Scrollbar = transform.Find("Scr_Scrollbar").GetComponent<Scrollbar>();
		
    }

    private void OnDestroy()
    {
        
    }
}
