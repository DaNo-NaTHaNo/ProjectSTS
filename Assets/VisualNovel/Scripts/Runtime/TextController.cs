using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class TextController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject textBoxBottom;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI speakerGroupText;
    [SerializeField] private TextMeshProUGUI speakerTextBottom;
    [SerializeField] private GameObject textBoxMonologue;
    [SerializeField] private TextMeshProUGUI speakerTextMonologue;
    [SerializeField] private GameObject branchButtonContainer;
    [SerializeField] private Button branchButtonPrefab;

    private Coroutine _typingCoroutine;
    private Action<int> _onBranchSelected;
    private bool isWaitingForBranchSelection = false;
    private TextMeshProUGUI _currentTargetText;

    public void HideAll()
    {
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = null;
        textBoxBottom.SetActive(false);
        textBoxMonologue.SetActive(false);
        branchButtonContainer.SetActive(false);
        isWaitingForBranchSelection = false;
    }

    public IEnumerator PlayDialogueLine(DialogueLine line, string displayMode)
    {
        if (displayMode == "Bottom")
        {
            textBoxBottom.SetActive(true);
            textBoxMonologue.SetActive(false);
            speakerNameText.text = line.speakerName;
            speakerGroupText.text = line.speakerGroup;
            _currentTargetText = speakerTextBottom;
        }
        else
        {
            textBoxBottom.SetActive(false);
            textBoxMonologue.SetActive(true);
            _currentTargetText = speakerTextMonologue;
        }

        _currentTargetText.text = line.speakerText;
        _typingCoroutine = StartCoroutine(TypewriterEffect(line.textSpeed));
        yield return _typingCoroutine;
    }

    public void SkipTyping()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
            if (_currentTargetText != null)
            {
                _currentTargetText.maxVisibleCharacters = _currentTargetText.text.Length;
            }
        }
    }

    public IEnumerator WaitForConfirmInput()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitUntil(() => Input.GetMouseButtonDown(0) && !isWaitingForBranchSelection);
        yield return null;
    }

    private IEnumerator TypewriterEffect(float textSpeed)
    {
        _currentTargetText.maxVisibleCharacters = 0;
        float delay = textSpeed > 0 ? 1.0f / textSpeed : 0;

        while (_currentTargetText.maxVisibleCharacters < _currentTargetText.text.Length)
        {
            _currentTargetText.maxVisibleCharacters++;
            yield return new WaitForSeconds(delay);
        }
        _typingCoroutine = null;
    }

    public void ShowBranches(List<string> branchTexts)
    {
        isWaitingForBranchSelection = true;
        branchButtonContainer.SetActive(true);
        foreach (Transform child in branchButtonContainer.transform) { Destroy(child.gameObject); }
        for (int i = 0; i < branchTexts.Count; i++)
        {
            Button newButton = Instantiate(branchButtonPrefab, branchButtonContainer.transform);
            newButton.GetComponentInChildren<TextMeshProUGUI>().text = branchTexts[i];
            int selectionIndex = i;
            newButton.onClick.AddListener(() => OnBranchButtonClicked(selectionIndex));
        }
    }
    public void SetBranchCallback(Action<int> callback) { _onBranchSelected = callback; }
    private void OnBranchButtonClicked(int index)
    {
        isWaitingForBranchSelection = false;
        branchButtonContainer.SetActive(false);
        _onBranchSelected?.Invoke(index);
        _onBranchSelected = null;
    }
}