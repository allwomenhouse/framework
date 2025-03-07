using Signum.Entities.Chart;

namespace Signum.Engine.Chart.Scripts;

public class MultiLinesChartScript : ChartScript                
{
    public MultiLinesChartScript(): base(D3ChartScript.MultiLines)
    {
        this.Icon = ChartScriptLogic.LoadIcon("multilines.png");
        this.Columns = new List<ChartScriptColumn>
        {
            new ChartScriptColumn("Horizontal Axis", ChartColumnType.Groupable),
            new ChartScriptColumn("Split Lines", ChartColumnType.Groupable) { IsOptional = true },
            new ChartScriptColumn("Height", ChartColumnType.Positionable) ,
            new ChartScriptColumn("Height 2", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Height 3", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Height 4", ChartColumnType.Positionable) { IsOptional = true },
            new ChartScriptColumn("Height 5", ChartColumnType.Positionable) { IsOptional = true }
        };
        this.ParameterGroups = new List<ChartScriptParameterGroup>
        {
            new ChartScriptParameterGroup()
            {
                new ChartScriptParameter("CompleteValues", ChartParameterType.Enum) { ColumnIndex = 0,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No|FromFilters") },
                new ChartScriptParameter("Scale", ChartParameterType.Enum) { ColumnIndex = 2,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
            },
            new ChartScriptParameterGroup("Margin")
            {
                new ChartScriptParameter("UnitMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 40m } },
            },
             new ChartScriptParameterGroup("Number")
            {
                new ChartScriptParameter("NumberMinWidth", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 20 } },
                new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } },
            },
           new ChartScriptParameterGroup("Circle")
            {
                new ChartScriptParameter("CircleAutoReduce", ChartParameterType.Enum) { ValueDefinition = EnumValueList.Parse("Yes|No") },
                new ChartScriptParameter("CircleRadius", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 5 } },
                new ChartScriptParameter("CircleStroke", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 2 } },
                new ChartScriptParameter("CircleRadiusHover", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 15 } },
            },
            new ChartScriptParameterGroup("Color Category")
            {
                new ChartScriptParameter("ColorCategory", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("category10|accent|dark2|paired|pastel1|pastel2|set1|set2|set3|BrBG[K]|PRGn[K]|PiYG[K]|PuOr[K]|RdBu[K]|RdGy[K]|RdYlBu[K]|RdYlGn[K]|Spectral[K]|Blues[K]|Greys[K]|Oranges[K]|Purples[K]|Reds[K]|BuGn[K]|BuPu[K]|OrRd[K]|PuBuGn[K]|PuBu[K]|PuRd[K]|RdPu[K]|YlGnBu[K]|YlGn[K]|YlOrBr[K]|YlOrRd[K]") },
                new ChartScriptParameter("ColorCategorySteps", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("3|4|5|6|7|8|9|10|11") }
            },
            new ChartScriptParameterGroup("Form")
            {
                new ChartScriptParameter("Interpolate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("linear|step-before|step-after|cardinal|monotone|basis|bundle") },
            }
        };
    }      
}                
