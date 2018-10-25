using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;
using Assets;
using System.Text;

public class Shikaku : MonoBehaviour
{
    // Shape
    enum S { Line, L, T, U, Plus, H, SmallS, SmallZ, LargeS, LargeZ, _2, _3, _4, _5, _6, _7 };

    // Direction
    enum D { Up, Right, Down, Left };

    const int Width = 6;
    const int Height = 6;

    const int OutOfBounds = -1;
    const int Empty = 0;

    public KMBombInfo Bomb;
    public KMBombModule Module;
    public GameObject Squares;
    public GameObject ActiveColor;
    public Material[] Materials;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _isSolved;
    private int[] _puzzle = new int[36];
    private int[] _grid = new int[36];
    private Dictionary<S, ShapeDef> _shapeDefs = new Dictionary<S, ShapeDef>()
    {
        { S.Line, new ShapeDef() { Shape = S.Line, Name = "Line", MinSize = 2, HintChars = "ABAB", MaxCount = 2 } },
        { S.L, new ShapeDef() { Shape = S.L, Name = "L", MinSize = 3, HintChars = "CDEF", MaxCount = 2 } },
        { S.T, new ShapeDef() { Shape = S.T, Name = "T", MinSize = 4, HintChars = "GHIJ", MaxCount = 2 } },
        { S.U, new ShapeDef() { Shape = S.U, Name = "U", MinSize = 5, HintChars = "KLMN", MaxCount = 2 } },
        { S.Plus, new ShapeDef() { Shape = S.Plus, Name = "Plus", MinSize = 5, HintChars = "OOOO", MaxCount = 1 } },
        { S.H, new ShapeDef() { Shape = S.H, Name = "H", MinSize = 7, HintChars = "QRQR", MaxCount = 1 } },
        { S.SmallS, new ShapeDef() { Shape = S.SmallS, Name = "Small S", MinSize = 4, HintChars = "STST", MaxCount = 1 } },
        { S.SmallZ, new ShapeDef() { Shape = S.SmallZ, Name = "Small Z", MinSize = 4, HintChars = "UVUV", MaxCount = 1 } },
        { S.LargeS, new ShapeDef() { Shape = S.LargeS, Name = "Large S", MinSize = 5, HintChars = "WXWX", MaxCount = 1 } },
        { S.LargeZ, new ShapeDef() { Shape = S.LargeZ, Name = "Large Z", MinSize = 5, HintChars = "YZYZ", MaxCount = 1 } },
        { S._2, new ShapeDef() { Shape = S._2, IsNumber = true, Name = "2", MinSize = 2, HintChars = "2222", MaxCount = 3 } },
        { S._3, new ShapeDef() { Shape = S._3, IsNumber = true, Name = "3", MinSize = 3, HintChars = "3333", MaxCount = 3 } },
        { S._4, new ShapeDef() { Shape = S._4, IsNumber = true, Name = "4", MinSize = 4, HintChars = "4444", MaxCount = 2 } },
        { S._5, new ShapeDef() { Shape = S._5, IsNumber = true, Name = "5", MinSize = 5, HintChars = "5555", MaxCount = 2 } },
        { S._6, new ShapeDef() { Shape = S._6, IsNumber = true, Name = "6", MinSize = 6, HintChars = "6666", MaxCount = 1 } },
        { S._7, new ShapeDef() { Shape = S._7, IsNumber = true, Name = "7", MinSize = 7, HintChars = "7777", MaxCount = 1 } }
    };
    private KMSelectable[] _buttons = new KMSelectable[36];
    private TextMesh[] _hints = new TextMesh[36];
    private List<Shape> _shapes = new List<Shape>();
    private List<int> _colors;
    private int _activeShape;
    private string _manual = "GWEKTYAIOUSDMQHJRLBXCVZNF";
    private int _lastCoordPressed = -1;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        for (int i = 0; i < 36; i++)
        {
            var j = i;
            _buttons[i] = Squares.transform.Find(i.ToString()).GetComponent<KMSelectable>();
            _buttons[i].OnInteract += delegate () { PressButton(j); return false; };
            _hints[i] = Squares.transform.Find(i.ToString()).transform.Find("Hint (" + i.ToString() + ")").GetComponent<TextMesh>();
            _hints[i].text = "";
        }

        // Random colors
        _colors = (new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9 }).Shuffle();
        _colors.Insert(0, 0);

        GeneratePuzzle();

        Debug.LogFormat("[Shikaku #{0}] Possible solution:", _moduleId);
        string msg;
        for (var i = 0; i < 6; i++)
        {
            msg = "";
            for (var j = 0; j < 6; j++) msg += _colors[_puzzle[i * 6 + j]];
            Debug.LogFormat("[Shikaku #{0}] {1}", _moduleId, msg);
        }
        var hints = new StringBuilder().Append(' ', 36);
        foreach (var shape in _shapes) hints[shape.HintNode] = shape.HintChar;
        Debug.LogFormat("[Shikaku #{0}] Shape data for Logfile Analyzer: {1}", _moduleId, hints);

        msg = "";
        foreach (var shape in _shapes)
        {
            msg += shape.ShapeType.Name + " in " + shape.HintNode + "\n";
        }
        DevLog(msg);

        Refresh();
    }

    private void PressButton(int i)
    {
        if (_isSolved) return;

        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch(.05f);

        // Pressing a hint
        foreach (var shape in _shapes)
        {
            if (shape.HintNode == i)
            {
                // When it's already the active shape
                if (shape.Number == _activeShape && !shape.ShapeType.IsNumber)
                {
                    shape.CurrentHintCorrect = !shape.CurrentHintCorrect;
                    _hints[shape.HintNode].text = shape.CurrentHintCorrect ? shape.HintChar.ToString() : shape.FakeHintChar.ToString();
                }
                else
                {
                    _activeShape = shape.Number;
                    ActiveColor.GetComponent<Renderer>().material = Materials[_colors[_grid[i]]];
                }
                CheckIfSolved();
                return;
            }
        }

        // Pressing another square
        if (_activeShape == 0) return;
        _grid[i] = _activeShape;
        Refresh();
        CheckIfSolved();
    }

    private void CheckIfSolved()
    {
        _isSolved = IsSolved();
        if (_isSolved)
        {
            Module.HandlePass();
            Debug.LogFormat("[Shikaku #{0}] Solved!", _moduleId);
        }
    }

    private void GeneratePuzzle()
    {
        // Try to generate puzzle
        var puzzleSuccess = false;
        var tryPuzzle = 0;
        while (true)
        {
            tryPuzzle++;

            _shapes = new List<Shape>();
            _puzzle = new int[36];
            foreach (var shapeDef in _shapeDefs) shapeDef.Value.Count = 0;

            // Add some shapes. Try a bunch of times, doesn't really matter how many succeed, we'll check some conditions afterwards.
            var tryShape = 0;
            while (tryShape < 10)
            {
                tryShape++;

                ShapeDef ShapeType;
                do ShapeType = _shapeDefs.ElementAt(Rnd.Range(0, 10)).Value;
                while (ShapeType.Count == ShapeType.MaxCount);
                var success = false;

                var tryShapeType = 0;
                while (tryShapeType < 10)
                {
                    tryShapeType++;
                    success = TryToAddShape(ShapeType, _shapes.Count + 1);
                    if (success) break;
                }

                DevLog((success ? "Succeeded" : "Failed") + " to add a " + ShapeType.Name + " in " + tryShapeType.ToString() + " tries");

                if (_shapes.Count == 6) break;
            }

            if (_shapes.Count < 4)
            {
                DevLog("Could not fit 4 shapes.");
                continue;
            }

            // Make sure we have some exotic shapes in the mix
            if (_shapeDefs[S.H].Count + _shapeDefs[S.Plus].Count + _shapeDefs[S.U].Count == 0)
            {
                DevLog("Only easy shapes.");
                continue;
            }

            // Extend the shapes
            int cursorNode;
            foreach (var shape in _shapes)
            {
                foreach (var extension in shape.Extensions)
                {
                    cursorNode = extension.Node;
                    while (Step(ref cursorNode, extension.Direction))
                    {
                        extension.Node = cursorNode;
                        shape.Nodes.Add(cursorNode);
                    }
                    foreach (var n in shape.Nodes) _puzzle[n] = shape.Number;
                }
            }

            // Identify the empty areas that are left
            var failed = false;
            for (var i = 0; i < _puzzle.Length; i++)
            {
                if (_puzzle[i] == 0)
                {
                    // Empty square found, let's see how big it is
                    var shape = GetConnectedArea(_puzzle, i);
                    shape.Number = _shapes.Count + 1;

                    if (shape.Nodes.Count < 2 || shape.Nodes.Count > 7)
                    {
                        DevLog("Empty areas too small or too big.");
                        failed = true;
                        break;
                    }
                    shape.ShapeType = _shapeDefs.ElementAt(8 + shape.Nodes.Count).Value;
                    _shapes.Add(shape);
                    if (_shapes.Count > 9)
                    {
                        DevLog("Too many shapes");
                        failed = true;
                        break;
                    }
                    shape.ShapeType.Count++;
                    if (shape.ShapeType.Count > shape.ShapeType.MaxCount)
                    {
                        DevLog("Too many shapes of size " + shape.ShapeType.Name);
                        failed = true;
                        break;
                    }

                    foreach (var node in shape.Nodes) _puzzle[node] = shape.Number;
                }
            }
            if (_shapeDefs[S._2].Count + _shapeDefs[S._3].Count + _shapeDefs[S._4].Count + _shapeDefs[S._5].Count + _shapeDefs[S._6].Count + _shapeDefs[S._7].Count == 0)
            {
                DevLog("No number shapes");
                failed = true;
            }

            if (failed) continue;
            puzzleSuccess = true;
            break;
        }

        if (!puzzleSuccess)
        {
            DevLog("Giving up :(");
            return;
        }

        DevLog("Generated puzzle in " + tryPuzzle.ToString() + " tries.");

        // Hints
        var sum = _shapeDefs.Select(shape => shape.Value.IsNumber ? shape.Value.Count * shape.Value.MinSize : 0).Sum();
        sum = (sum - 1) % 4 + 1;
        foreach (var shape in _shapes)
        {
            // Random hint location within shape
            shape.HintNode = shape.Nodes[Rnd.Range(0, shape.Nodes.Count)];
            shape.HintChar = shape.ShapeType.HintChars[(int)shape.Direction];

            // Number shapes only have one hint
            if (shape.ShapeType.IsNumber)
            {
                _hints[shape.HintNode].text = shape.HintChar.ToString();
            }

            // Symbol shapes have two hints that toggle
            else
            {
                var hint = _manual.IndexOf(shape.HintChar);
                int fakeHint = 0;
                var numbers = Enumerable.Range(0, 25).ToList();

                switch (sum)
                {
                    case 1:
                        if (hint / 5 == 4) fakeHint = hint - 5;
                        else fakeHint = numbers.Where(n => n / 5 > hint / 5 && n != hint + 5).PickRandom();
                        break;
                    case 2:
                        if (hint % 5 == 0) fakeHint = hint + 1;
                        else fakeHint = numbers.Where(n => n % 5 < hint % 5 && n != hint - 1).PickRandom();
                        break;
                    case 3:
                        if (hint / 5 == 0) fakeHint = hint + 5;
                        else fakeHint = numbers.Where(n => n / 5 < hint / 5 && n != hint - 5).PickRandom();
                        break;
                    case 4:
                        if (hint % 5 == 4) fakeHint = hint - 1;
                        else fakeHint = numbers.Where(n => n % 5 > hint % 5 && n != hint + 1).PickRandom();
                        break;
                }
                shape.FakeHintChar = _manual[fakeHint];
                shape.CurrentHintCorrect = Rnd.Range(0, 2) == 0;
                _hints[shape.HintNode].text = shape.CurrentHintCorrect ? shape.HintChar.ToString() : shape.FakeHintChar.ToString();
            }

            // Color the hint nodes
            _grid[shape.HintNode] = shape.Number;
        }
    }

    private Shape GetConnectedArea(int[] grid, int startNode)
    {
        var shape = new Shape();
        var checkNeighbours = new Stack<int>();
        checkNeighbours.Push(startNode);
        while (checkNeighbours.Count > 0)
        {
            var node = checkNeighbours.Pop();
            shape.Nodes.Add(node);
            foreach (D direction in Enum.GetValues(typeof(D)))
            {
                var newNode = GetNode(node, direction);
                if (checkNeighbours.Contains(newNode)) continue;
                if (shape.Nodes.Contains(newNode)) continue;
                if (newNode == OutOfBounds) continue;
                if (grid[newNode] != grid[startNode]) continue;
                checkNeighbours.Push(newNode);
            }
        }

        return shape;
    }

    private bool TryToAddShape(ShapeDef shapeDef, int number)
    {
        // Random starting node
        var shape = new Shape()
        {
            ShapeType = shapeDef,
            Number = number,
            Direction = RandomDirection()
        };

        int startNode;
        do startNode = Rnd.Range(0, _puzzle.Length);
        while (_puzzle[startNode] != Empty);
        var cursorNode = startNode;
        shape.Nodes.Add(cursorNode);
        var direction = shape.Direction;
        var numNodes = 0;

        // Construct basic shape
        switch (shapeDef.Shape)
        {
            case S.Line:
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case S.L:
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                direction = Turn180(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnCcw(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case S.T:
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnCcw(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                direction = Turn180(direction);
                Step(ref cursorNode, direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case S.U:
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                direction = Turn180(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnCcw(direction);
                var maxNodes = Rnd.Range(3, Width);
                numNodes = 1;
                while (Step(ref cursorNode, direction))
                {
                    shape.Nodes.Add(cursorNode);
                    numNodes++;
                    if (numNodes == maxNodes) break;
                }
                if (numNodes < 3) return false;
                direction = TurnCcw(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case S.Plus:
                for (var i = 0; i < 4; i++)
                {
                    if (!Step(ref cursorNode, direction)) return false;
                    shape.Nodes.Add(cursorNode);
                    shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                    Step(ref cursorNode, Turn180(direction));
                    direction = TurnCw(direction);
                }
                break;
            case S.H:
                direction = TurnCw(direction);
                maxNodes = Rnd.Range(3, 6);
                numNodes = 1;
                while (Step(ref cursorNode, direction))
                {
                    shape.Nodes.Add(cursorNode);
                    numNodes++;
                    if (numNodes == maxNodes) break;
                }
                if (numNodes < 3) return false;
                direction = TurnCw(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                direction = Turn180(direction);
                Step(ref cursorNode, direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                cursorNode = startNode;
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                direction = Turn180(direction);
                Step(ref cursorNode, direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case S.SmallS:
                direction = TurnCw(direction);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnCcw(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnCw(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case S.SmallZ:
                direction = TurnCw(direction);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnCw(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnCcw(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case S.LargeS:
                direction = TurnCw(direction);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnCcw(direction);
                maxNodes = Rnd.Range(3, 6);
                numNodes = 1;
                while (Step(ref cursorNode, direction))
                {
                    shape.Nodes.Add(cursorNode);
                    numNodes++;
                    if (numNodes == maxNodes) break;
                }
                if (numNodes < 3) return false;
                direction = TurnCw(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case S.LargeZ:
                direction = TurnCw(direction);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnCw(direction);
                maxNodes = Rnd.Range(3, 6);
                numNodes = 1;
                while (Step(ref cursorNode, direction))
                {
                    shape.Nodes.Add(cursorNode);
                    numNodes++;
                    if (numNodes == maxNodes) break;
                }
                if (numNodes < 3) return false;
                direction = TurnCcw(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
        }

        foreach (var node in shape.Nodes) _puzzle[node] = shape.Number;
        _shapes.Add(shape);
        shape.ShapeType.Count++;

        return true;
    }

    private D RandomDirection()
    {
        var values = Enum.GetValues(typeof(D));
        return (D)values.GetValue(Rnd.Range(0, values.Length));
    }

    private D TurnCw(D direction)
    {
        return (D)(((int)direction + 1) % 4);
    }

    private D Turn180(D direction)
    {
        return (D)(((int)direction + 2) % 4);
    }

    private D TurnCcw(D direction)
    {
        return (D)(((int)direction + 3) % 4);
    }

    private int GetNode(int node, D direction)
    {
        switch (direction)
        {
            case D.Up:
                if (node / Height == 0) return OutOfBounds;
                return node - Height;
            case D.Right:
                if (node % Width == Width - 1) return OutOfBounds;
                return node + 1;
            case D.Down:
                if (node / Height == Height - 1) return OutOfBounds;
                return node + Height;
            case D.Left:
                if (node % Width == 0) return OutOfBounds;
                return node - 1;
            default:
                return OutOfBounds;
        }
    }

    // Try to step in a direction. If it's out of bounds, don't step and return false.
    // If not, check if it's what you expect (default empty).
    // If it is, make the step and return true.
    // If not, don't make the step and return false.
    private bool Step(ref int node, D direction, int[] grid = null, int expect = Empty)
    {
        // Check the puzzle grid by default. Provide another grid if you want.
        if (grid == null) grid = _puzzle;

        var newNode = node;
        switch (direction)
        {
            case D.Up:
                if (node / Height == 0) return false;
                newNode = node - Height;
                break;
            case D.Right:
                if (node % Width == Width - 1) return false;
                newNode = node + 1;
                break;
            case D.Down:
                if (node / Height == Height - 1) return false;
                newNode = node + Height;
                break;
            case D.Left:
                if (node % Width == 0) return false;
                newNode = node - 1;
                break;
            default:
                return false;
        }

        if (grid[newNode] != expect) return false;

        node = newNode;
        return true;
    }

    private void Refresh()
    {
        for (var i = 0; i < _grid.Length; i++)
        {
            _buttons[i].GetComponent<Renderer>().material = Materials[_colors[_grid[i]]];
        }
    }


    private bool IsSolved()
    {
        // Check if all hints are set correctly
        foreach (var shape in _shapes)
            if (!shape.ShapeType.IsNumber && !shape.CurrentHintCorrect)
                return false;

        // Check all shapes
        int[] overlay = new int[36];
        int node, storeNode, count;
        foreach (var shape in _shapes)
        {
            var direction = shape.Direction;
            var number = shape.Number;
            switch (shape.ShapeType.Shape)
            {
                case S.Line:
                    node = FindStartNode(_grid, number, TurnCw(direction));
                    overlay[node] = shape.Number;
                    count = 1;
                    while (Step(ref node, direction, _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    break;
                case S.L:
                    node = FindStartNode(_grid, number, Turn180(direction));
                    overlay[node] = shape.Number;
                    count = 1;
                    while (Step(ref node, Turn180(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    count = 1;
                    while (Step(ref node, TurnCw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    break;
                case S.T:
                    node = FindStartNode(_grid, number, direction);
                    overlay[node] = shape.Number;
                    count = 1;
                    while (Step(ref node, direction, _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    storeNode = node;
                    count = 1;
                    while (Step(ref node, TurnCcw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    node = storeNode;
                    count = 1;
                    while (Step(ref node, TurnCw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    break;
                case S.U:
                    node = FindStartNode(_grid, number, direction);
                    overlay[node] = shape.Number;
                    storeNode = node;
                    count = 1;
                    while (Step(ref node, direction, _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    node = storeNode;
                    count = 1;
                    while (Step(ref node, TurnCcw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 3) return false;
                    count = 1;
                    while (Step(ref node, direction, _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    break;
                case S.Plus:
                    node = FindStartNode(_grid, number, direction);
                    overlay[node] = shape.Number;
                    storeNode = Empty;
                    count = 1;
                    while (Step(ref node, direction, _grid, number)) {
                        if (storeNode == Empty) // Check if the crossing of lines is here
                        {
                            if (Step(ref node, TurnCw(direction), _grid, number))
                            {
                                Step(ref node, TurnCcw(direction), _grid, number); // Step back to continue the first line
                                storeNode = node; // Store where the crossing is
                            }
                        }
                        overlay[node] = number;
                        count++;
                    }
                    if (count < 3) return false;
                    if (storeNode == Empty) return false;
                    if (storeNode == node) return false; // That's a T, not a Plus
                    node = storeNode;
                    count = 1;
                    while (Step(ref node, TurnCcw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    node = storeNode;
                    count = 1;
                    while (Step(ref node, TurnCw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    break;
                case S.H:
                    node = FindStartNode(_grid, number, TurnCw(direction));
                    overlay[node] = shape.Number;
                    storeNode = Empty;
                    count = 1;
                    while (Step(ref node, direction, _grid, number))
                    {
                        if (storeNode == Empty) // Check if the crossing of lines is here
                        {
                            if (Step(ref node, TurnCw(direction), _grid, number))
                            {
                                Step(ref node, TurnCcw(direction), _grid, number); // Step back to continue the first line
                                storeNode = node; // Store where the bar is
                            }
                        }
                        overlay[node] = number;
                        count++;
                    }
                    if (count < 3) return false;
                    if (storeNode == Empty) return false;
                    if (storeNode == node) return false; // That's a U, not an H
                    node = storeNode;
                    count = 1;
                    while (Step(ref node, TurnCw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 3) return false;
                    storeNode = node;
                    count = 1;
                    while (Step(ref node, direction, _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    node = storeNode;
                    count = 1;
                    while (Step(ref node, Turn180(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    break;
                case S.SmallS:
                    node = FindStartNode(_grid, number, TurnCw(direction));
                    overlay[node] = shape.Number;
                    count = 1;
                    while (Step(ref node, TurnCw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    if (!Step(ref node, direction, _grid, number)) return false;
                    overlay[node] = number;
                    count = 1;
                    while (Step(ref node, TurnCw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    break;
                case S.SmallZ:
                    node = FindStartNode(_grid, number, TurnCw(direction));
                    overlay[node] = shape.Number;
                    count = 1;
                    while (Step(ref node, TurnCw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    if (!Step(ref node, Turn180(direction), _grid, number)) return false;
                    overlay[node] = number;
                    count = 1;
                    while (Step(ref node, TurnCw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    break;
                case S.LargeS:
                    node = FindStartNode(_grid, number, TurnCw(direction));
                    overlay[node] = shape.Number;
                    count = 1;
                    while (Step(ref node, TurnCw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    count = 1;
                    while (Step(ref node, direction, _grid, number)) { overlay[node] = number; count++; }
                    if (count < 3) return false;
                    count = 1;
                    while (Step(ref node, TurnCw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    break;
                case S.LargeZ:
                    node = FindStartNode(_grid, number, TurnCw(direction));
                    overlay[node] = shape.Number;
                    count = 1;
                    while (Step(ref node, TurnCw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    count = 1;
                    while (Step(ref node, Turn180(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 3) return false;
                    count = 1;
                    while (Step(ref node, TurnCw(direction), _grid, number)) { overlay[node] = number; count++; }
                    if (count < 2) return false;
                    break;
                case S._2:
                case S._3:
                case S._4:
                case S._5:
                case S._6:
                case S._7:
                    var checkShape = GetConnectedArea(_grid, shape.HintNode);
                    if (checkShape.Nodes.Count != shape.ShapeType.MinSize) return false;
                    foreach (var n in checkShape.Nodes) overlay[n] = shape.Number;
                    break;
            }
            DevLog(shape.ShapeType.Name + " is correct.");
        }

        // All shapes are correct. Now see if all squares are covered.
        // (in the OVERLAY, which only has the clean shapes)
        if (overlay.Contains(Empty)) {
            DevLog("Still squares left that are empty or don't belong to the shape.");
            return false;
        }

        return true;
    }

    private int FindStartNode(int[] grid, int number, D scanTowards)
    {
        int startOfLineNode = 0, cursorNode;

        if (scanTowards == D.Left) startOfLineNode = Width - 1;
        else if (scanTowards == D.Up) startOfLineNode = Width * Height - 1;
        else if (scanTowards == D.Right) startOfLineNode = Width * (Height - 1);

        cursorNode = startOfLineNode;

        while (true) // At least the hint node has the correct number
        {
            // Check current node
            if (_grid[cursorNode] == number) return cursorNode;

            // Step in scan direction
            cursorNode = GetNode(cursorNode, TurnCcw(scanTowards));

            // Go to next line if needed
            if (cursorNode == OutOfBounds)
            {
                cursorNode = GetNode(startOfLineNode, scanTowards);
                startOfLineNode = cursorNode;
            }
        }
    }

    private void DevLog(string message)
    {
        //Debug.Log("[DEBUG] " + message);
    }

    class ShapeDef
    {
        public S Shape { get; set; }
        public bool IsNumber { get; set; }
        public string Name { get; set; }
        public int MinSize { get; set; }
        public string HintChars { get; set; }
        public int MaxCount { get; set; }
        public int Count { get; set; }
    }

    class Extension
    {
        public int Node { get; set; }
        public D Direction { get; set; }
    }

    class Shape
    {
        public ShapeDef ShapeType { get; set; }
        public List<int> Nodes { get; set; }
        public int HintNode { get; set; }
        public char HintChar { get; set; }
        public char FakeHintChar { get; set; }
        public bool CurrentHintCorrect { get; set; }
        public D Direction { get; set; }
        public int Number { get; set; }

        // List of possible extensions, they can be visited in a later stage to fill the gaps
        public List<Extension> Extensions { get; set; }

        public Shape()
        {
            Nodes = new List<int>();
            Extensions = new List<Extension>();
        }
    }

    private string TwitchHelpMessage = @"Use '!{0} press a1' to press the button with a certain coordinate. Use '!{0} press a1 d r u l' to press the button, and then press the buttons in a direction from the previously pressed button.";

    IEnumerator ProcessTwitchCommand(string command)
    {
        var parts = command.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if ( parts.Length > 1 && parts[0] == "press" && parts.Skip(1).All(part => (part.Length == 2 && "abcdef".Contains(part[0]) && "123456".Contains(part[1])) || (part.Length == 1 && "udlr".Contains(part))))
        {
            yield return null;

            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length == 2)
                {
                    var part = parts[i];
                    int x = char.ToUpper(part[0]) - 'A';
                    int y = Int32.Parse(part[1].ToString()) - 1;
                    int coord = 6 * y + x; 
                    PressButton(coord);
                    _lastCoordPressed = coord; 
                }
                else if (parts[i].Length == 1)
                {
                    if (_lastCoordPressed == -1) { yield return "sendtochaterror You haven't specified a coordinate yet!"; yield break; }
                    if (parts[i] == "u")
                    {
                        int coord = _lastCoordPressed - 6;
                        if (coord < 0) { yield return "sendtochaterror You can't go outside the border!"; yield break; }
                        else { PressButton(coord); _lastCoordPressed = coord; }
                    }
                    else if (parts[i] == "d")
                    {
                        int coord = _lastCoordPressed + 6;
                        if (coord > 35) { yield return "sendtochaterror You can't go outside the border!"; yield break; }
                        else { PressButton(coord); _lastCoordPressed = coord; }
                    }
                    else if (parts[i] == "l")
                    {
                        int coord = _lastCoordPressed - 1;
                        if (coord / 6 != _lastCoordPressed / 6) { yield return "sendtochaterror You can't go outside the border!"; yield break; }
                        else { PressButton(coord); _lastCoordPressed = coord; }
                    }
                    else if (parts[i] == "r")
                    {
                        int coord = _lastCoordPressed + 1;
                        if (coord / 6 != _lastCoordPressed / 6) { yield return "sendtochaterror You can't go outside the border!"; yield break; }
                        else { PressButton(coord); _lastCoordPressed = coord; }
                    }
                }

                yield return new WaitForSeconds(.1f);
            }
        }
    }
}
