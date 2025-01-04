using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[System.Serializable] 
public class CustomText1 //I use this for unity serialization
{
    public string[] text_Input;
}


public class FakeShellInterface : MonoBehaviour
{
    public static FakeShellInterface instance;




    [Space]

    public bool text_ForceCapsLock;

    [Space]

    public KeyCode key_TurnOn;
    public KeyCode key_ShutDown;

    [Space]

    public Vector3 terminal_PoVOffset;
    public float terminal_ActivationTransitionSpeed;
    public float terminal_ShutDownTransitionSpeed;

    [Space]

    //[HideInInspector]public Movement player_CharController; ---put your own controller
    public Camera player_Camera;

    [Space]

    public float terminal_MaxActivationDistance;
    [Space]

    [Header("MySettings")]
    public Transform computerScreen;
    public float distance = 3f;
    public float height = 1f;

        [Space]

    public float intro_TimeToPrintLine;
    //  It's the time that passes before printing the next intro string.
    public int text_MaxStringsOnScreen;
    //   You have to manually set up the max number of visible lines...
    public int text_MaxCharsInString;
    //   ... and the max number of chars per line to keep the terminal text inside the "fake" screen.
    [Space]

    public char cursor_Char;
    //  Choose your favorite cursor char. Default: █
    public float cursor_BlinkingTime;
    //  Choose cursor blinking time.

    //----//

    private List<string> outputText = new List<string>();
    //  Basically, this script uses a list of strings. Every frame last string in the list is updated with your input chars.
    //  When you press Enter a new string is added to list and, if you have reached lines cap (text_MaxStringsOnScreen), the oldest line is removed (FIFO: first in, first out).

    private Text screen;

    private int actualLine;
    //  This value goes from 0 to text_MaxStringsOnScreen - 1 (it's the list index).
    private int fakeLine;
    //  Just esthetics. It's a sequential number shown in each line.
    private string lineIntro;
    //  This string contains the first chars of every line.
    private string originalPassword;

    private bool terminalIsRunning;
    private bool terminalStarting;
    private bool terminalIsIdling;
    private bool cursorVisible;
    //  Variables used to control the terminal state.
    private bool lockCamera;
    private bool oldPositionSaved;

    public string[] text_Intro;

    public string[] text_Help;

    public string[] text_Help2;

    private Vector3 oldPlayerPoVPosition;
    
    private Quaternion oldPlayerPoVRotation;
    //  The player's camera has to return in its original position when the terminal is shut down.

    //[SerializeField]
    //public List<CustomText[]> fakeFiles;

    public List<CustomText1> fakeFiles;
    private FakeShell shell;


    void Awake()
    {
        instance = this;
    }
    void Start()
    {
        List<string[]> FormatedData = new List<string[]>();
        foreach(CustomText1 txt in fakeFiles)
        {
            FormatedData.Add(txt.text_Input);
        }
        shell = new FakeShell("Tue Jan 12 22:29:30 2028", "User", "Test laptop", FormatedData);
        if(("" + cursor_Char) == " ")
        {
            cursor_Char = '█';
        }


        actualLine = 0;
        fakeLine = 0;

        cursorVisible = false;

        terminalIsIdling = true;
        terminalStarting = false;
        terminalIsRunning = false;
        oldPositionSaved = false;

        lockCamera = false;

        screen = this.GetComponent<Text>();

        terminal_PoVOffset = new Vector3(terminal_PoVOffset.x, terminal_PoVOffset.y, -terminal_PoVOffset.z);

        StartCoroutine("TerminalIdlingCursor");
    }

    void LateUpdate()
    {
        if(Input.GetKeyDown(key_TurnOn) && (this.transform.position - player_Camera.transform.position).magnitude < terminal_MaxActivationDistance && terminalIsIdling)
        {
            StopCoroutine("TerminalIdlingCursor");
            lockCamera = true;

            terminalIsIdling = false;
            terminalStarting = true;
            terminalIsRunning = true;

            StartCoroutine("TerminalStart");
            StartCoroutine("TerminalRunningCursor");
        }
        if(lockCamera)
        {
            /*if(player_CharController != null)
            {
                player_CharController.enabled = false;
            }*/

            if(!oldPositionSaved)
            {
                oldPlayerPoVPosition = player_Camera.transform.position;

                oldPositionSaved = true;
            }
            //player_Camera.gameObject.GetComponent<MouseLook>().enabled = false; ----replace with the script u use
            player_Camera.transform.position = computerScreen.position - computerScreen.forward * distance + computerScreen.up * height;
            player_Camera.transform.LookAt(computerScreen); // Rotate camera to computer screen

             

            if(!terminalStarting && terminalIsRunning)
            {
                TerminalInput();
            }
        }

        if(Input.GetKeyDown(key_ShutDown) && terminalIsRunning)
        {
            ShutdownTerminal();
        }
    }
    void ExitTerminalTransition()
    {
        lockCamera = false;

        //while(player_Camera.transform.position != oldPlayerPoVPosition)
        //{
            //player_Camera.transform.position = Vector3.Lerp(player_Camera.transform.position, oldPlayerPoVPosition, terminal_ShutDownTransitionSpeed * Time.deltaTime);
            //player_Camera.transform.rotation = Quaternion.Lerp(player_Camera.transform.rotation, oldPlayerPoVRotation, terminal_ShutDownTransitionSpeed * Time.deltaTime);
        player_Camera.transform.Find("WeaponCamera").gameObject.SetActive(true);
        //player_Camera.gameObject.GetComponent<MouseLook>().enabled = true;
        //player_CharController.enabled = true;


            //yield return new WaitForEndOfFrame();
        //}

        player_Camera.transform.position = oldPlayerPoVPosition;


        oldPositionSaved = false;

    }

    //----//

    private void ShutdownTerminal()
    {
        StopAllCoroutines();

        terminalIsIdling = true;
        terminalStarting = false;
        terminalIsRunning = false;

        Cursor.lockState = CursorLockMode.Locked;

        StartCoroutine("TerminalIdlingCursor");

        //StartCoroutine("ExitTerminalTransition");
        ExitTerminalTransition();
    }
    private void TerminalInput()
    {
        foreach (char c in Input.inputString)
        {
            if (c == '\b') //   Has backspace/delete been pressed?
            {
                if (outputText[actualLine].Length > lineIntro.Length)
                {
                    if(outputText[actualLine].Contains("" + cursor_Char) && outputText[actualLine].Length > lineIntro.Length + 1)
                    {
                        outputText[actualLine] = outputText[actualLine].Substring(0, outputText[actualLine].Length - 2) + cursor_Char;
                    }
                    else if(!outputText[actualLine].Contains("" + cursor_Char))
                    {
                        outputText[actualLine] = outputText[actualLine].Substring(0, outputText[actualLine].Length - 1);
                    }
                }
            }
            else if ((c == '\n') || (c == '\r')) // Has enter/return been pressed?
            {
                string formatedInput = outputText[actualLine].Replace("" + cursor_Char, "").Substring(lineIntro.Length);
                if(outputText[actualLine].Contains("" + cursor_Char))
                {
                    string temp = "";
                    foreach(char letter in outputText[actualLine])
                    {
                        if(letter != cursor_Char)
                        temp += letter;
                    }
                    outputText[actualLine] = temp;

                }
                if(formatedInput == "help 1" || formatedInput == "help")
                {
                    print("help 1");
                    foreach(string line in text_Help)
                    {
                        AddLineToList(line);
                    }
                }
                else if(formatedInput == "help 2")
                {
                    foreach(string line in text_Help2)
                    {
                        AddLineToList(line);
                    }
                }

                else
                {
                    shell.HandleInput(formatedInput);
                    foreach(string s in shell.MyOutput){
                        string[] allLines = s.Split("<br>");
                        print(allLines.Length);
                        foreach(string s2 in allLines){
                            AddLineToList(s2);
                        }
                    }
                }



                fakeLine++;
                lineIntro = "" + fakeLine + ".  ";

                AddLineToList(lineIntro);

            }
            else
            {
                if(outputText[actualLine].Contains("" + cursor_Char))
                {
                    string temp = "";
                    foreach(char letter in outputText[actualLine])
                    {
                        if(letter != cursor_Char)
                        temp += letter;
                    }

                    if(outputText[actualLine].Length < text_MaxCharsInString)
                    {
                        outputText[actualLine] = temp + c + cursor_Char;
                    }
                }
                else
                {
                    if(outputText[actualLine].Length < text_MaxCharsInString)
                    {
                        outputText[actualLine] += c;
                    }
                }
            }
        }
        screen.text = GenerateTextFromList(outputText);
    }



    IEnumerator TerminalStart()
    {
        outputText = new List<string>();
        fakeLine = 0;

        foreach(string line in text_Intro)
        {
            outputText.Add(line);
            actualLine++;
            screen.text = GenerateTextFromList(outputText);
            yield return new WaitForSeconds(intro_TimeToPrintLine);
        }

        AddLineToList("");

        lineIntro = "" + fakeLine + ".  ";
        AddLineToList(lineIntro);
        screen.text = GenerateTextFromList(outputText);

        terminalStarting = false;
    }

    IEnumerator TerminalIdlingCursor()
    {   
        while(terminalIsIdling)
        {
            outputText = new List<string>();
            if(!terminalStarting)
            {
                if(!cursorVisible)
                {
                    outputText.Add("" + cursor_Char);
                    cursorVisible = true;
                }
                else
                {  
                    cursorVisible = false;
                }
            }

            screen.text = GenerateTextFromList(outputText);

            yield return new WaitForSeconds(cursor_BlinkingTime);
        }
    }

    IEnumerator TerminalRunningCursor()
    {
        while(terminalIsRunning)
        {
            if(!terminalStarting)
            {
                if(!cursorVisible)
                {
                    bool cursorAlreadyExist = false;

                    foreach(char letter in outputText[outputText.Count - 1])
                    {
                        if(letter == cursor_Char)
                        {
                            cursorAlreadyExist = true;
                        }
                    }

                    if(!cursorAlreadyExist)
                    {
                        outputText[outputText.Count - 1] += cursor_Char;
                    }
                    cursorVisible = true;
                }
                else
                {
                    string temp = "";
                    foreach(char letter in outputText[outputText.Count - 1])
                    {
                        if(letter != cursor_Char)
                        {
                            temp += letter;
                        }
                    }

                    outputText[outputText.Count - 1] = temp;
                    cursorVisible = false;
                }
            }
            yield return new WaitForSeconds(cursor_BlinkingTime);
        }

    }

    //----//

    private void UpdateLineIndex()
    {
        if(actualLine + 1 <= text_MaxStringsOnScreen)
        {
            actualLine++;
        }
        ScrollText();
    }

    //----//


    public void AddLineToList(string newLine)
    {
        outputText.Add(newLine);
        UpdateLineIndex();
    }

    private void ScrollText()
    {
        if(outputText.Count > text_MaxStringsOnScreen)
        {
            List<string> temp = new List<string>();

            for(int i = 1; i < text_MaxStringsOnScreen + 1; i++)
            {
                temp.Add(outputText[i]);
            }

            outputText = temp;

            actualLine--;
        }
    }

    private string GenerateTextFromList(List<string> lines)
    {
        actualLine = -1;

        string textToPrint = "";

        foreach(string line in lines)
        {
            actualLine++;
            if(text_ForceCapsLock)
            {
                textToPrint += (line.ToUpper() + "\n");
            }
            else
            {
                textToPrint += (line + "\n");
            }
        }

        return textToPrint;
    }


}
