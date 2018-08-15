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
    const int ShapeN = 7;
    const int ShapeZ = 8; // <- BigS, BigZ, SmallS, SmallZ
    const int Shape2 = 9;
    const int Shape3 = 10;
    const int Shape4 = 11;
    const int Shape5 = 12;
    const int Shape6 = 13;
    const int Shape7 = 14;

    const int Width = 6;
    const int Height = 6;

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
        new ShapeType() { Shape = ShapeLine, Name = "Line", MinSize = 2 },
        new ShapeType() { Shape = ShapeL, Name = "L", MinSize = 3 },
        new ShapeType() { Shape = ShapeT, Name = "T", MinSize = 4 },
        new ShapeType() { Shape = ShapeU, Name = "U", MinSize = 5 },
        new ShapeType() { Shape = ShapePlus, Name = "Plus", MinSize = 5 },
        new ShapeType() { Shape = ShapeSquare, Name = "Square", MinSize = 4 },
        new ShapeType() { Shape = ShapeH, Name = "H", MinSize = 7 },
        new ShapeType() { Shape = ShapeN, Name = "N", MinSize = 4 },
        new ShapeType() { Shape = ShapeZ, Name = "Z", MinSize = 5 },
        new ShapeType() { Shape = Shape2, Name = "2", MinSize = 2 },
        new ShapeType() { Shape = Shape3, Name = "3", MinSize = 3 },
        new ShapeType() { Shape = Shape4, Name = "4", MinSize = 4 },
        new ShapeType() { Shape = Shape5, Name = "5", MinSize = 5 },
        new ShapeType() { Shape = Shape6, Name = "6", MinSize = 6 },
        new ShapeType() { Shape = Shape7, Name = "7", MinSize = 7 }
    };
    private KMSelectable[] _buttons = new KMSelectable[36];
    private List<Shape> _shapes = new List<Shape>();

    void Start()
    {
        //_moduleId = _moduleIdCounter++;

        for (int i = 0; i < 36; i++)
        {
            _buttons[i] = Squares.transform.Find(i.ToString()).GetComponent<KMSelectable>();
            var j = i;
            _buttons[i].OnInteract += delegate () { PressButton(j); return false; };
        }

        GeneratePuzzle();
        Refresh();
    }

    private void PressButton(int i)
    {
    }

    private void GeneratePuzzle()
    {
        int shapeNumber = 0;
        while (shapeNumber < 5)
        {
            shapeNumber++;
            //var shapeType = _shapeTypes[Rnd.Range(0, _shapeTypes.Length)];
            var shapeType = _shapeTypes[ShapeLine];
            var tries = 1;
            while (!TryToAddShape(shapeType, shapeNumber)) tries++;
            Debug.Log("Tries: " + tries.ToString());
        }
    }

    private bool TryToAddShape(ShapeType shapeType, int shapeNumber)
    {
        // Random starting node
        int startNode;
        do startNode = Rnd.Range(0, _grid.Length);
        while (_grid[startNode] != 0);

        var shape = new Shape()
        {
            ShapeType = shapeType,
            Color = Colors[shapeNumber],
            Direction = RandomDirection(),
            HintNode = startNode,
            Nodes = new List<int>()
        };

        // Draw basic shape
        switch (shapeType.Shape)
        {
            case ShapeLine:
                int node = startNode;
                while (node != -1 && _grid[node] == 0)
                {
                    shape.Nodes.Add(node);
                    node = Step(node, shape.Direction);
                }
                if (shape.Nodes.Count < 2) return false;
                break;
        }
        _shapes.Add(shape);
        foreach (var node in shape.Nodes) _grid[node] = shapeNumber;

        return true;
    }

    private Direction RandomDirection()
    {
        var values = Enum.GetValues(typeof(Direction));
        return (Direction)values.GetValue(Rnd.Range(0, values.Length));
    }

    private int Step(int node, Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                if (node / Width == 0) return -1;
                return node - Width;
            case Direction.Right:
                if (node % Width == Width - 1) return -1;
                return node + 1;
            case Direction.Down:
                if (node / Width == Height - 1) return -1;
                return node + Width;
            case Direction.Left:
                if (node % Width == 0) return -1;
                return node - 1;
            default:
                return -1;
        }
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

    struct ShapeType
    {
        public int Shape { get; set; }
        public string Name { get; set; }
        public int MinSize { get; set; }
    }

    struct Shape
    {
        public ShapeType ShapeType { get; set; }
        public List<int> Nodes { get; set; }
        public int HintNode { get; set; }
        public Direction Direction { get; set; }
        public Material Color { get; set; }
    }
}
