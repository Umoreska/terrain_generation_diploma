using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Mathematics;

public class SliderValueText : MonoBehaviour
{
    private TMP_Text text;

    private void Start() {
        text = GetComponent<TMP_Text>();
    }

    public void UpadteSliderValue(float value) {
        if (value % 1 == 0) { // Якщо дробової частини немає, повертаємо ціле число як рядок
            text.text = ((int)value).ToString();
        } else { // Якщо дробова частина є, повертаємо з одним десятковим знаком
            text.text = value.ToString("F1");
        }
    }
}
