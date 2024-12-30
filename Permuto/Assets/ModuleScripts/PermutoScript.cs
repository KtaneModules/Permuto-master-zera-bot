using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using System;

public class PermutoScript : MonoBehaviour {

	// Variables
	[Header("Stuff")]
	public KMBombInfo bomb;
	public KMAudio Audio;

	[Header("Objects")]
	public KMSelectable nextButton;
	public KMSelectable submitButton;
	public PermutationGenerator generator;
	public TextMesh submitButtonText;
	public TextMesh displayText;
	public KMSoundOverride.SoundEffect clickSound;
	[Header("Game Variables")]
	//Logging
	static int moduleIdCounter = 1;
	int moduleId;
	bool solved = false;

	// game stuff
	int stage = 1;
	public List<int> correctState;
	// stage 1
	int currentQuery = 0;
	
	public List<KeyValuePair<List<int>, int>> givenInformation;
	// stage 2
	List<int> currentPermutation = new List<int>{0,1,2,3,4};


	public Material[] colors; // the color materials
	public string[] colorStrings;
	public MeshRenderer[] buttonRenderers;
	public KMSelectable[] buttons;

	// misc necessary funtions

	string IntListToColorString(List<int> state){
		string str = "";
		for (int i = 0; i<state.Count; i++){
			str += colorStrings[state[i]];
			if (i < state.Count-1){
				str += ", ";
			}
		}
		return str;
	}

	// functions for color swapping

	int selectedIndex = -1;
	void Swap(int indexA, int indexB){
		// swap the two indices in currentPermutation
		int temp = currentPermutation[indexA];
		currentPermutation[indexA] = currentPermutation[indexB];
		currentPermutation[indexB] = temp;
		RenderStage2();
	}

	void SelectColor(int index){
		if (stage == 2){
			if (selectedIndex == -1){
				selectedIndex = index;
				// TODO add additional highlight
			} else if (index == selectedIndex){ // de-select item
				selectedIndex = -1;
			} else { // already selected first, swap two items
				Swap(selectedIndex, index);
				selectedIndex = -1;
			}
		}
		buttons[index].AddInteractionPunch(0.75f);
		Audio.PlayGameSoundAtTransform(clickSound,buttons[index].transform);
	}

	// Rendering the stages

	void RenderStage1(int queryNumber){
		displayText.text = givenInformation[queryNumber].Value.ToString();
		for (int i = 0; i<5; i++){
			Material color = colors[givenInformation[queryNumber].Key[i]];
			buttonRenderers[i].material = color;
		}
		// disable highlights
	}

	void RenderStage2(){ // render current state
		for (int i = 0; i<5; i++){
			Material color = colors[currentPermutation[i]];
			buttonRenderers[i].material = color;
		}
		// enable highlights
	}

	// Buttons

	void PressNextButton(){
		if (stage == 2){
			stage = 1;
			currentQuery = -1; // will be set to 0 shortly after
			// TODO disable buttons & highlights
			submitButtonText.text = "READY";
		}

		currentQuery = (currentQuery+1)%givenInformation.Count;
		RenderStage1(currentQuery);

		nextButton.AddInteractionPunch(0.75f);
		Audio.PlayGameSoundAtTransform(clickSound,nextButton.transform);
	}

	void PressSubmitButton(){
		if (stage == 1){ // Stage 1 --> Stage 2
			stage = 2;
			RenderStage2();
			// TODO enable buttons & highlights
			submitButtonText.text = "SUBMIT";
			displayText.text = "";
		} else if (!solved) { // submission
			int total = 0;
			for (int i = 0; i < correctState.Count; i++){
				if (currentPermutation[i] == correctState[i]){
					total++;
				}
			}

			if (total == correctState.Count){
				// disarm
				solved = true;
				GetComponent<KMBombModule>().HandlePass();

				Debug.LogFormat("[Permuto #{0}] Correct permutation {1} submitted; module disarmed.", moduleId, IntListToColorString(currentPermutation));
			} else {
				// strike
				Debug.LogFormat("[Permuto #{0}] Incorrect permutation {1} submitted; module gave a strike.", moduleId, IntListToColorString(currentPermutation));
				GetComponent<KMBombModule>().HandleStrike();
			}
		}

		submitButton.AddInteractionPunch(0.75f);
		Audio.PlayGameSoundAtTransform(clickSound,submitButton.transform);
	}

	// Initialization stuff

	void Awake(){
		moduleId = moduleIdCounter++;
		for (int i = 0; i < buttons.Length; i++){
			// i love how all languages make me do this :grr:
			int new_i = i;
			KMSelectable bt = buttons[new_i];
			bt.OnInteract += delegate(){SelectColor(new_i); return false;};
		}
		nextButton.OnInteract += delegate(){PressNextButton(); return false;};
		submitButton.OnInteract += delegate(){PressSubmitButton(); return false;};
	}

	void Start () {
		correctState = generator.GenerateSolution(currentPermutation);
		givenInformation = generator.GenerateGivenInformation(correctState);
		RenderStage1(currentQuery);

		// debug stuff

		List<string> detailedGivenInformation = new List<string>();
		foreach (KeyValuePair<List<int>,int> pair in givenInformation){
			string str = IntListToColorString(pair.Key);
			str += " → " + pair.Value.ToString();
			detailedGivenInformation.Add(str);
		}

		Debug.LogFormat("[Permuto #{0}] Correct Permutation is {1}.", moduleId, IntListToColorString(correctState));
		for (int i = 0; i<detailedGivenInformation.Count; i++){
			Debug.LogFormat("[Permuto #{0}] Clue #{1}: {2}",moduleId,(i+1).ToString(),detailedGivenInformation[i]);
		}
	}
	
	// Twitch plays
	#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Use !{0} next to press the NEXT button, use !{0} ready or !{0} submit to press the submit button, and use !{0} swap X Y to swap the colors at indices X and Y (starting with 1). If you have multiple pairs you may do !{0} swap X Y Y Z etc.";
	#pragma warning restore 414

	
	IEnumerator ProcessTwitchCommand(string command) {
        // Implemented by Quinn Wuest

        // The help message should include the mod ID in its command. This is used by typing {0}. That way, the help command will have "!1 submit" instead of "!submit".

		// I removed the logging message that logs what the TP command sent to chat was. Twitch Plays does this already.

        // To each of the Regexes, I added "RegexOptions.CultureInvariant", which ensures that unicode letters in different language sets don't interfere with the code.
        // For example, in english, we have lowercase i and capital I.
        // In Turkish, there is lowercase i and capital İ, as well as lowercase ı and capital I.
        // This addition makes sure this distinction isn't a problem.

        // Also to each of the Regexes, I added (press\s+)? before the command. This allows an optional "press" to be put at the start of the command.

        // I changed the call of "Press<whatever>Button" to "<button>.OnInteract".
        // That way, the behavior executed by TP is identical to what happens on a real bomb. Doing this allows audio sounds to play or interaction punches to trigger.

        // A "yield return null" before a button press sends a signal to the TP handler that the command is valid. Not including it causes issues.
        // "yield return null" should NOT be present if the given command is invalid. This avoids the TP handler focusing on the module for a command that would otherwise be invalid.
        // "yield return sendtochaterror" may be used to send an error message to Twitch Chat clarifying what may be wrong about input.
        // "yield break" stops the TP handler immediately. This may be used for an invalid command or for any other reasons proposed by the module.

        command = command.ToLowerInvariant();

        var match = Regex.Match(command, @"^\s*(press\s+)?next\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (match.Success) {
			yield return null;
			nextButton.OnInteract();
			yield break;
		}

		match = Regex.Match(command, @"^\s*(press\s+)?ready\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (match.Success) {
			if (stage != 1)
			{
				yield return "sendtochaterror The READY button cannot be pressed yet. Command ignored.";
				yield break;
			}
			yield return null;
			submitButton.OnInteract();
			yield break;
		}

		match = Regex.Match(command, @"^\s*(press\s+)?submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (match.Success) {
            if (stage != 2)
            {
                yield return "sendtochaterror The SUBMIT button cannot be pressed yet. Command ignored.";
                yield break;
            }
            yield return null;
            submitButton.OnInteract();
            yield break;
        }

		match = Regex.Match(command, @"^\s*swap\s+(?<nums>[12345,; ]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		// A bit complicated here.
		// What this Regex does is adds a group labeled "nums" that can contain any of the characters: "12345,; ".
		// The + indicates that any number of these characters are valid.
		if (match.Success)
		{
			if (stage != 2)
			{
				yield return "sendtochaterror You may not swap tiles yet. Command ignored.";
				yield break;
			}
			var nums = match.Groups["nums"].Value;
			// This line fetches the "nums" group and converts it to a string to iterate over.
			var list = new List<int>();
			// Prepare a list which will be added to executed at the end.
			for (int i = 0; i < nums.Length; i++)
			{
				int ix = "12345,; ".IndexOf(nums[i]);
				if (ix > 4) // If the index of the character is greater than 4 (alternatively, equal to ",", ";", or " ")...
					continue; // ..ignore it, and go to the next iteration.
				list.Add(ix); // Add the index to the list.
			}
			if (list.Count % 2 != 0) // Count the number of items in the list. If it's odd, ignore the command, since swaps always require two presses.
			{
				yield return "sendtochaterror Even number of swaps detected. Command ignored.";
				yield break;
			}
			yield return null; // Now that the command is valid, add a "yield return null" and execute the command.
			for (int i = 0; i < list.Count; i++)
			{
				buttons[list[i]].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
        }
	}
	
	// Autosolver.
	private IEnumerator TwitchHandleForcedSolve()
	{
		// If we are in the first stage, go to the second stage.
		if (stage == 1)
		{
			submitButton.OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
		
		// Go through all five buttons.
		for (int i = 0; i < 5; i++)
		{
			// If the color is already in the correct position, ignore it.
			if (currentPermutation[i] == correctState[i])
				continue;

			// Press the button at the current position.
			buttons[i].OnInteract();
			yield return new WaitForSeconds(0.1f);

			// Find and press the button that belongs in this position.
			buttons[currentPermutation.IndexOf(correctState[i])].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
		// Submit the solution.
		submitButton.OnInteract();
		yield break;
	}
}
