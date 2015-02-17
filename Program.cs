﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AdditiveNC
{
    class Program
    {
        //Some simple classes to make data formatting a smidge easier.
        class GeomData
        {
            public GeomMetaData MetaData = new GeomMetaData();
        }
        class point
        {
            public int x =-1;
            public int y =-1;
            public point(int inx, int iny) { x = inx; y = iny; }
        }
        class GeomMetaData
        {
            public double power = -1;
            public double speed = -1;
            public double focus = -1;
        }
        class CLIHatches : GeomData
        {
            public int id = -1;
            public int numberofhatches = -1;
            public List<CLIHatch> hatches = new List<CLIHatch>();
        }
        class CLIHatch : IFormattable
        {
            public int startx = -1;
            public int starty = -1;
            public int endx = -1;
            public int endy = -1;
            public String ToString(string format, IFormatProvider formatprovider) { return String.Format("{0},{1} - {2},{3}", startx, starty, endx, endy); }
        }
        class Polyline : GeomData
        {
            public int id = -1;
            public int direction = -1;
            public int numberofpoints = -1;
            public List<point> points = new List<point>();
        }
        class Layer
        {
            public int height = -1;
            public List<GeomData> operations = new List<GeomData>();
        }
        static void Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("Usage: {0} [filename]", System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                return;
            }
            double unitsmultiplier = .001; //Units default to mm, or .001m. 
            List<Layer> layers = parseFile(args[0],ref unitsmultiplier);
            CreateNCFile(layers,args[0]+".NC",unitsmultiplier);
        }
        static List<Layer> parseFile(String filename,ref double unitsmultiplier)
        {
            Queue<string> lines = new Queue<string>(System.IO.File.ReadAllLines(filename));
            string line = lines.Dequeue();
            while (!(line.Contains("$$GEOMETRYSTART"))) //Parse header
            {
                if (line.Contains("$$UNITS/"))
                {
                    unitsmultiplier = .001 * Convert.ToDouble(line.Substring("$$UNITS/".Length));
                }
                line = lines.Dequeue();
            }
            List<Layer> layers = new List<Layer>();
            while ((!line.Contains("$$GEOMETRYEND"))) //Parse Geometry
            {
                if (line.Contains("$$LAYER"))
                {
                    Layer foo = parselayer(lines);
                    foo.height = Convert.ToInt32(line.Substring("$$LAYER/".Length));
                    Console.WriteLine("Layer added with {0} operations", foo.operations.Count);
                    layers.Add(foo);
                }
                line = lines.Dequeue();
            }
            Console.WriteLine("{0} Layers processed.", layers.Count);
            return layers;
        }
        static Layer parselayer(Queue<string> lines)
        {
            Layer thisLayer = new Layer();
            while ((!(lines.Peek().Contains("$$LAYER")||lines.Peek().Contains("$$GEOMETRYEND"))) && (!lines.Peek().Equals(""))) //don't want to take more than we need.
            {
                GeomMetaData gmd = new GeomMetaData();
                string line = lines.Dequeue();
                gmd.power = Convert.ToDouble(line.Substring("$$POWER/".Length));
                line = lines.Dequeue();
                gmd.speed = Convert.ToDouble(line.Substring("$$SPEED/".Length));
                line = lines.Dequeue();
                gmd.focus = Convert.ToDouble(line.Substring("$$FOCUS/".Length));
                line = lines.Dequeue();
                GeomData tmp;
                if (line.Contains("$$POLYLINE"))
                {
                    tmp=parsepolyline(line);
                }
                else if (line.Contains("$$HATCHES"))
                {
                    tmp = parsehatches(line);
                }
                else tmp = new GeomData(); //Error case?
                tmp.MetaData = gmd;
                thisLayer.operations.Add(tmp);
            }
            return thisLayer;
        }
        static Polyline parsepolyline(String line)
        {
            Polyline p = new Polyline();
            List<String> values = new List<String>(line.Split(','));
            //Parse ID
            if (values[0].Contains("$$POLYLINE/"))
            {
                values[0] = values[0].Substring("$$POLYLINE/".Length);

            }
            p.id = Convert.ToInt32(values[0]);
            values.RemoveAt(0);

            //Parse Direction
            p.direction = Convert.ToInt32(values[0]);
            values.RemoveAt(0);
            //Parse count.
            p.numberofpoints = Convert.ToInt32(values[0]);
            values.RemoveAt(0);
            for (int i = 0; i < p.numberofpoints; i++)
            {
                int x = Convert.ToInt32(values[0]);
                int y = Convert.ToInt32(values[1]);
                point tmpoint = new point(x, y);
                p.points.Add(tmpoint);
                values.RemoveRange(0, 2);
            }
            return p;
        }
        static CLIHatches parsehatches(String line)
        {
            CLIHatches h = new CLIHatches();
            List<String> values = new List<String>(line.Split(','));
            if (values[0].Contains("$$HATCHES/"))
            {
                values[0] = values[0].Substring("$$HATCHES/".Length);

            }
            h.id = Convert.ToInt32(values[0]);
            values.RemoveAt(0);
            //Parse count.
            h.numberofhatches = Convert.ToInt32(values[0]);
            values.RemoveAt(0);
            for (int i = 0; i < h.numberofhatches; i++)
            {
                CLIHatch hatch = new CLIHatch();
                hatch.startx = Convert.ToInt32(values[0]);
                hatch.starty = Convert.ToInt32(values[1]);
                hatch.endx = Convert.ToInt32(values[2]);
                hatch.endy = Convert.ToInt32(values[3]);
                h.hatches.Add(hatch);
                values.RemoveRange(0, 4);
            }
            return h;
        }
        static void CreateNCFile(List<Layer>layers,string outname,double unitsmultiplier)
        {
            STEPNCLib.AptStepMaker asm = new STEPNCLib.AptStepMaker();
            asm.NewProjectWithCCandWP(outname,4,"Main");
            asm.Millimeters();
            int i=0;
            foreach(Layer layer in layers)
            {
                
                asm.Workingstep(String.Format("Layer {0}",i));
                foreach(GeomData operation in layer.operations)
                {   
                    if(operation is CLIHatches)
                    {
                        CLIHatches tmp = operation as CLIHatches;
                        foreach(CLIHatch hatch in tmp.hatches)
                        {
                            asm.GoToXYZ("", hatch.startx * unitsmultiplier, hatch.starty * unitsmultiplier, layer.height * unitsmultiplier);
                            asm.GoToXYZ("", hatch.endx * unitsmultiplier, hatch.endy * unitsmultiplier, layer.height * unitsmultiplier);
                        }
                    }
                }
                i++;
            }
            asm.SaveAsModules(outname);
            return;
        }
    }
}
