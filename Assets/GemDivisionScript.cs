using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GemDivisionScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public List<KMSelectable> cuts;
    public KMSelectable button;
    public GameObject[] cutpieces;
    public Renderer[] gems;
    public Renderer[] cutrends;
    public Renderer[] cushrends;
    public Transform[] centrestand;
    public Renderer[] spinrings;
    public Material[] gemmats;
    public Material[] cutmats;
    public Material[] cushmats;

    private int randcut;
    private int distgems;
    private List<int> gemtypes = new List<int> { };
    private List<int> cutselect = new List<int> { };

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        randcut = Random.Range(0, 20);
        distgems = Random.Range(3, 6);
        for (int i = 0; i < distgems; i++)
        {
            gemtypes.Add(i);
            gemtypes.Add(i);
        }
        for (int i = distgems; i < 10; i++)
        {
            int r = Random.Range(0, distgems);
            gemtypes.Add(r);
            gemtypes.Add(r);
        }
        gemtypes = gemtypes.Shuffle();
        for (int i = 0; i < 20; i++)
            gems[i].material = gemmats[gemtypes[i]];
        cutrends[randcut].enabled = true;
        cutrends[randcut].material = cutmats[0];
        for (int i = 0; i < 2; i++)
            spinrings[i].material = cushmats[0];
        Debug.LogFormat("[Gem Division #{0}] The arrangement of gems in clockwise order is: [{1}]", moduleID, string.Join("", Enumerable.Range(0, 20).Select(x => "PORSE"[gemtypes[(x + randcut) % 20]].ToString()).ToArray()));
        StartCoroutine(Spin());
        foreach (KMSelectable cut in cuts)
        {
            int c = cuts.IndexOf(cut);
            cut.OnInteract = delegate ()
            {
                if (!moduleSolved && !cutselect.Contains(c))
                {
                    if (c == randcut)
                    {
                        if (cutselect.Count() > 1)
                        {
                            Audio.PlaySoundAtTransform("Deselect", cut.transform);
                            while(cutselect.Count() > 1)
                            {
                                cutrends[cutselect[0]].enabled = false;
                                cutselect.RemoveAt(0);
                            }
                        }
                    }
                    else
                    {
                        Audio.PlaySoundAtTransform("Select", cut.transform);
                        cutselect.Add(c);
                        cutrends[c].enabled = true;
                        if (cutselect.Count() > distgems)
                        {
                            int r = cutselect[0];
                            cutrends[r].enabled = false;
                            cutselect.RemoveAt(0);
                        }
                    }
                }
                return false;
            };
        }
        button.OnInteract = delegate ()
        {
            if (!moduleSolved && cutselect.Count() > 0)
            {
                button.AddInteractionPunch();
                Audio.PlaySoundAtTransform("Press", button.transform);
                spinrings[0].material = cushmats[1];
                spinrings[1].material = cushmats[2];
                for (int i = 2; i < 4; i++)
                    spinrings[i].material = cutmats[0];
                foreach (int c in cutselect)
                    cutrends[c].material = cutmats[0];
                StartCoroutine(Submit(cutselect));
            }
            return false;
        };
    }

    private IEnumerator Spin()
    {
        float e = 0;
        while (true)
        {
            e = Time.deltaTime * 120;
            centrestand[1].Rotate(0, 0, e);
            yield return null;
        }
    }

    private IEnumerator Submit(List<int> cut)
    {
        List<int> sub = new List<int> { };
        moduleSolved = true;
        foreach (int c in cut)
            sub.Add(c);
        centrestand[2].localPosition -= new Vector3(0, 0, 0.2f);
        yield return new WaitForSeconds(0.5f);
        sub.Add(randcut);
        bool oddcut = true;
        int[][] split = new int[2][] { new int[distgems], new int[distgems] };
        Audio.PlaySoundAtTransform("Grind", centrestand[2]);
        string logcuts = string.Empty;
        bool[] up = new bool[20];
        for(int i = 0; i < 20; i++)
        {
            int c = (randcut + i) % 20;
            centrestand[2].Rotate(0, 0, 4.5f);
            if(sub.Count() > 0 && sub.Contains(c))
            {
                oddcut ^= true;
                sub.Remove(c);
                if(c != randcut)
                logcuts += "], [";
            }
            up[c] = !oddcut;
            logcuts += "PORSE"[gemtypes[c]].ToString();
            split[oddcut ? 1 : 0][gemtypes[c]]++;
            cushrends[c].material = cushmats[oddcut ? 2 : 1];
            yield return new WaitForSeconds(0.1f);
        }
        Debug.LogFormat("[Gem Division #{0}] The gems have been divided into the following groups: [{1}]", moduleID, logcuts);
        yield return new WaitForSeconds(0.5f);
        string[] loggems = new string[2];
        for (int i = 0; i < distgems; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                loggems[j] += split[j][i] + " ";
                loggems[j] += new string[] { "pearl", "onyx", "rub", "sapphire", "emerald" }[i];
                if (split[j][i] > 1)
                    loggems[j] += new string[] { "s", "", "ies", "s", "s" }[i];
                else if (i == 2)
                    loggems[j] += "y";
                loggems[j] += i == distgems - 1 ? "." : ", ";
            }
        }
        Debug.LogFormat("[Gem Division #{0}] Obec receives: {1}", moduleID, loggems[0]);
        Debug.LogFormat("[Gem Division #{0}] Terzhu receives: {1}", moduleID, loggems[1]);
        if (split[0].SequenceEqual(split[1]))
        {
            Debug.LogFormat("[Gem Division #{0}] The gods are appeased. The vault has opened.", moduleID);
            module.HandlePass();
            StartCoroutine(Solve(up));
        }
        else
        {
            int[] anger = Enumerable.Range(0, distgems).Select(x => split[0][x] < split[1][x] ? 1 : (split[0][x] > split[1][x] ? -1 : 0)).ToArray();
            if (anger.Any(x => x > 0))
            {
                Audio.PlaySoundAtTransform("Storm", transform);
                string[] angems = new string[] { "pearls", "onyx", "rubies", "sapphires", "emeralds" }.Where((x, i) => i < distgems && anger[i] > 0).ToArray();
                Debug.LogFormat("[Gem Division #{0}] Obec is not pleased with his share of {1}.", moduleID, string.Join(", ", angems));
            }
            if (anger.Any(x => x < 0))
            {
                Audio.PlaySoundAtTransform("Quake", transform);
                string[] angems = new string[] { "pearls", "onyx", "rubies", "sapphires", "emeralds" }.Where((x, i) => i < distgems && anger[i] < 0).ToArray();
                Debug.LogFormat("[Gem Division #{0}] Terzhu is not pleased with his share of {1}.", moduleID, string.Join(", ", angems));
            }
            foreach (Renderer c in cushrends)
                c.material = cushmats[0];
            for(int i = 0; i < cutselect.Count(); i++)
            {
                int c = cutselect[i];
                if (c != randcut)
                {
                    cutrends[c].material = cutmats[1];
                    cutrends[c].enabled = false;
                }
            }
            cutselect.Clear();
            spinrings[0].material = cushmats[0];
            spinrings[1].material = cushmats[0];
            for (int i = 2; i < 4; i++)
                spinrings[i].material = cutmats[1];
            for(int i = 0; i < 5; i++)
            {
                centrestand[2].Rotate(0, 0, -18);
                yield return new WaitForSeconds(0.1f);
            }
            centrestand[2].localPosition += new Vector3(0, 0, 0.2f);
            module.HandleStrike();
            moduleSolved = false;
        }
    }

    private IEnumerator Solve(bool[] ring)
    {
        Audio.PlaySoundAtTransform("Unlock", transform);
        centrestand[0].localPosition += new Vector3(0, 0.023f, 0);
        for (int i = 0; i < 20; i++)
            cutrends[i].enabled = false;
        yield return new WaitForSeconds(0.5f);
        float e = 0;
        while(e < 2)
        {
            e += Time.deltaTime;
            for(int i = 0; i < 20; i++)
            {
                int c = (randcut + i) % 20;
                if (ring[c])
                    cutpieces[c].transform.localPosition = new Vector3(0, e * e * 2, -1);
                else
                    cutpieces[c].transform.localPosition = new Vector3(0, 0, Mathf.Lerp(-1, -0.2f, e / 2));
            }
            yield return null;
        }
        for (int i = 0; i < 20; i++)
            cutpieces[i].SetActive(false);
    }

#pragma warning disable 414
    private string TwitchPlaysHelpMessage = "!{0} cut <1-19> [Divides the ring at the specified numbers of spaces clockwise around the ring, starting from the original cut. Separate multiple cuts with spaces.] | !{0} undo [Removes all but the most recent cut.] | !{0} submit";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        string[] commands = command.ToLowerInvariant().Split(' ');
        yield return null;
        if(commands[0] == "undo")
        {
            cuts[randcut].OnInteract();
            yield break;
        }
        if(commands[0] == "submit")
        {
            button.OnInteract();
            yield break;
        }
        if(commands[0] == "cut")
        {
            if(commands.Length < 2)
            {
                yield return "sendtochaterror!f Invalid command length. Position must be specified.";
                yield break;
            }
            int[] p = new int[commands.Length - 1];
            for(int i = 0; i < commands.Length - 1; i++)
            {
                if(!int.TryParse(commands[i + 1], out p[i]) || p[i] < 1 || p[i] > 19)
                {
                    yield return "sendtochaterror!f \"" + commands[i + 1] + "\" is an invalid position.";
                    yield break;
                }
            }
            for(int i = 0; i < commands.Length - 1; i++)
            {
                cuts[(p[i] + randcut) % 20].OnInteract();
                yield return new WaitForSeconds(0.2f);
            }
        }
        else
            yield return "sendtochaterror!f \"" + commands[0] + "\" is an invalid command.";
    }
}
