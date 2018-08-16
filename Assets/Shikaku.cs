using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KmHelper;
using Rnd = UnityEngine.Random;

public class Shikaku : MonoBehaviour
{
    const int ShapeLine = 0;
    const int ShapeL = 1;
    const int ShapeT = 2;
    const int ShapeU = 3;
    const int ShapePlus = 4;
    const int ShapeSquare = 5;
    const int ShapeH = 6;
    const int ShapeSmallS = 8;
    const int ShapeSmallZ = 9;
    const int ShapeLargeS = 10;
    const int ShapeLargeZ = 11;
    const int Shape2 = 12;
    const int Shape3 = 13;
    const int Shape4 = 14;
    const int Shape5 = 15;
    const int Shape6 = 16;
    const int Shape7 = 17;

    const int Width = 6;
    const int Height = 6;

    const int OutOfBounds = -1;
    const int Empty = 0;

    enum Direction { Up, Right, Down, Left };

    public KMBombInfo Bomb;
    public KMSelectable Module;
    public GameObject Squares;
    public Material[] Colors;

    //private int _moduleId;
    //private static int _moduleIdCounter = 1;
    private int[] _grid = new int[36];
    private ShapeType[] _shapeTypes = new ShapeType[]
    {
        new ShapeType() { Shape = ShapeLine, Name = "Line", MinSize = 2, HintChars = "ABAB" },
        new ShapeType() { Shape = ShapeL, Name = "L", MinSize = 3, HintChars = "CDEF" },
        new ShapeType() { Shape = ShapeT, Name = "T", MinSize = 4, HintChars = "GHIJ" },
        new ShapeType() { Shape = ShapeU, Name = "U", MinSize = 5, HintChars = "KLMN" },
        new ShapeType() { Shape = ShapePlus, Name = "Plus", MinSize = 5, HintChars = "OOOO" },
        new ShapeType() { Shape = ShapeSquare, Name = "Square", MinSize = 4, HintChars = "PPPP" },
        new ShapeType() { Shape = ShapeH, Name = "H", MinSize = 7, HintChars = "QRQR" },
        new ShapeType() { Shape = ShapeSmallS, Name = "Small S", MinSize = 4, HintChars = "STST" },
        new ShapeType() { Shape = ShapeSmallZ, Name = "Small Z", MinSize = 4, HintChars = "UVUV" },
        new ShapeType() { Shape = ShapeLargeS, Name = "Large S", MinSize = 5, HintChars = "WXWX" },
        new ShapeType() { Shape = ShapeLargeZ, Name = "Large Z", MinSize = 5, HintChars = "YZYZ" },
        new ShapeType() { Shape = Shape2, Name = "2", MinSize = 2, HintChars = "2222" },
        new ShapeType() { Shape = Shape3, Name = "3", MinSize = 3, HintChars = "3333" },
        new ShapeType() { Shape = Shape4, Name = "4", MinSize = 4, HintChars = "4444" },
        new ShapeType() { Shape = Shape5, Name = "5", MinSize = 5, HintChars = "5555" },
        new ShapeType() { Shape = Shape6, Name = "6", MinSize = 6, HintChars = "6666" },
        new ShapeType() { Shape = Shape7, Name = "7", MinSize = 7, HintChars = "7777" }
    };
    private KMSelectable[] _buttons = new KMSelectable[36];
    private TextMesh[] _hints = new TextMesh[36];
    private List<Shape> _shapes = new List<Shape>();

    void Start()
    {
        //_moduleId = _moduleIdCounter++;

        for (int i = 0; i < 36; i++)
        {
            var j = i;
            _buttons[i] = Squares.transform.Find(i.ToString()).GetComponent<KMSelectable>();
            _buttons[i].OnInteract += delegate () { PressButton(j); return false; };
            _hints[i] = Squares.transform.Find(i.ToString()).transform.Find("Hint (" + i.ToString() + ")").GetComponent<TextMesh>();
            _hints[i].text = "";
        }

        GeneratePuzzle();
        Refresh();
    }

    private void PressButton(int i)
    {
    }

    private void GeneratePuzzle()
    {
        var puzzleTries = 0;

        // Add some shapes
        while (_shapes.Count < 5 && puzzleTries < 100)
        {
            puzzleTries++;
            //var shapeType = _shapeTypes[Rnd.Range(0, _shapeTypes.Length)];
            var shapeType = _shapeTypes[ShapeL];
            var shapeTries = 0;
            var success = false;
            while (shapeTries < 100)
            {
                shapeTries++;
                success = TryToAddShape(shapeType, _shapes.Count + 1);
                if (success) break;
            }
            DevLog((success ? "Succeeded" : "Failed") + " to add a " + shapeType.Name + " in " + shapeTries.ToString() + " tries");
        }

        // Fill in the gaps
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
                foreach (var node in shape.Nodes) _grid[node] = shape.Color;
            }
        }

        // Random hints
        foreach (var shape in _shapes)
        {
            shape.HintNode = shape.Nodes[Rnd.Range(0, shape.Nodes.Count)];
            _hints[shape.HintNode].text = shape.ShapeType.HintChars[(int)shape.Direction].ToString();
        }
    }

    private bool TryToAddShape(ShapeType shapeType, int color)
    {
        // Random starting node
        var shape = new Shape()
        {
            ShapeType = shapeType,
            Color = color,
            Direction = RandomDirection()
        };

        int startNode;
        do startNode = Rnd.Range(0, _grid.Length);
        while (_grid[startNode] != Empty);
        var cursorNode = startNode;
        shape.Nodes.Add(cursorNode);
        var direction = shape.Direction;
        var nodeCount = 0;

        // Construct basic shape
        switch (shapeType.Shape)
        {
            case ShapeLine:
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                while (Step(ref cursorNode, direction))
                {
                    shape.Nodes.Add(cursorNode);
                }
                if (shape.Nodes.Count < 2)
                {
                    DevLog("Failed to add a " + shapeType.Name + " at " + startNode + " directed " + shape.Direction + "; only room for 1.");
                    return false;
                }
                break;
            case ShapeL:
                var startFromTop = Rnd.Range(0f, 1f) < .5; // Start drawing from the top of the L or from the right
                direction = startFromTop ? Turn180(direction) : TurnLeft(direction);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                while (Step(ref cursorNode, direction))
                {
                    shape.Nodes.Add(cursorNode);
                }
                if (shape.Nodes.Count < 2)
                {
                    DevLog("Failed to add a " + shapeType.Name + " at " + startNode + " directed " + shape.Direction + "; only room for 1.");
                    return false;
                }
                direction = startFromTop ? TurnLeft(direction) : TurnRight(direction);
                nodeCount = 1;
                while (Step(ref cursorNode, direction))
                {
                    shape.Nodes.Add(cursorNode);
                    nodeCount++;
                }
                if (nodeCount < 2)
                {
                    DevLog("Failed to add a " + shapeType.Name + " at " + startNode + " directed " + shape.Direction
                        + ", part 2 going " + direction + "; only room for 1");
                    return false;
                }
                break;
        }

        foreach (var node in shape.Nodes) _grid[node] = shape.Color;
        _shapes.Add(shape);

        return true;
    }

    private Direction RandomDirection()
    {
        var values = Enum.GetValues(typeof(Direction));
        return (Direction)values.GetValue(Rnd.Range(0, values.Length));
    }

    private Direction TurnRight(Direction direction)
    {
        return (Direction)(((int)direction + 1) % 4);
    }

    private Direction Turn180(Direction direction)
    {
        return (Direction)(((int)direction + 2) % 4);
    }

    private Direction TurnLeft(Direction direction)
    {
        return (Direction)(((int)direction + 3) % 4);
    }

    private bool Step(ref int node, Direction direction)
    {
        var newNode = node;
        switch (direction)
        {
            case Direction.Up:
                if (node / Width == 0) return false;
                newNode = node - Width;
                break;
            case Direction.Right:
                if (node % Width == Width - 1) return false;
                newNode = node + 1;
                break;
            case Direction.Down:
                if (node / Width == Height - 1) return false;
                newNode = node + Width;
                break;
            case Direction.Left:
                if (node % Width == 0) return false;
                newNode = node - 1;
                break;
            default:
                return false;
        }

        if (_grid[newNode] != Empty)
            return false;

        node = newNode;
        return true;
    }

    private void Refresh()
    {
        for (var i = 0; i < _grid.Length; i++)
        {
            _buttons[i].GetComponent<Renderer>().material = Colors[_grid[i]];
        }
        string log = "";
        for (var i = 0; i < _grid.Length; i++)
            log += _grid[i].ToString() + (i % Width == Width - 1 ? "\n" : "");
        Debug.Log(log);
    }

    private void DevLog(string message)
    {
        Debug.Log(message);
    }

    class ShapeType
    {
        public int Shape { get; set; }
        public string Name { get; set; }
        public int MinSize { get; set; }
        public string HintChars { get; set; }
    }

    class Extension
    {
        public int Node { get; set; }
        public Direction Direction { get; set; }
    }

    class Shape
    {
        public ShapeType ShapeType { get; set; }
        public List<int> Nodes { get; set; }
        public int HintNode { get; set; }
        public Direction Direction { get; set; }
        public int Color { get; set; }
        // List of possible extensions, they can be visited in a later stage to fill the gaps
        public List<Extension> Extensions { get; set; }

        public Shape()
        {
            Nodes = new List<int>();
            Extensions = new List<Extension>();
        }
    }
}
