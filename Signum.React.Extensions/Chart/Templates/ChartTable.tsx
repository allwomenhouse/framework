import * as React from 'react'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { ResultTable, ColumnOptionParsed, OrderOptionParsed, OrderType, ResultRow, hasAggregate, ColumnOption, FilterOptionParsed, withoutAggregate } from '@framework/FindOptions'
import { ChartRequestModel, ChartColumnEmbedded } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import { toFilterOptions } from '@framework/Finder';
import { ChartRow } from '../ChartClient';
import { ChartColumn } from './ChartColumn';
import { TypeInfo } from '@framework/Reflection'
import { FullscreenComponent } from './FullscreenComponent'

interface ChartTableProps {
  resultTable: ResultTable;
  chartRequest: ChartRequestModel;
  lastChartRequest: ChartRequestModel;
  onOrderChanged: () => void;
  onReload?: (e: React.MouseEvent<any>) => void;
}

export default function ChartTableComponent(p: ChartTableProps) {
  function handleHeaderClick(e: React.MouseEvent<any>, col: ColumnOptionParsed) {
    var chartCol = p.chartRequest.columns.map(mle => mle.element)
      .firstOrNull(a => a.token != null && a.token.token != null && a.token.token.fullKey == col.token!.fullKey);

    if (chartCol) {
      ChartClient.handleOrderColumn(p.chartRequest, chartCol, e.shiftKey);
      p.onOrderChanged();
    }
  }


  function handleOnDoubleClick(e: React.MouseEvent<HTMLTableRowElement>, row: ResultRow) {
    const lcr = p.lastChartRequest!;

    if (row.entity) {

      window.open(Navigator.navigateRoute(row.entity!));

    } else {

      const filters = lcr.filterOptions.map(f => withoutAggregate(f)!).filter(Boolean);
      const columns: ColumnOption[] = [];

      lcr.columns.filter(a => a.element.token).map((a, i) => {

        const t = a.element.token!.token!;

        if (!hasAggregate(t)) {
          filters.push({
            token: t,
            operation: "EqualTo",
            value: row.columns[i],
            frozen: false
          } as FilterOptionParsed);
        }

        if (t.parent != undefined) //Avoid Count and simple Columns that are already added
        {
          if (t.queryTokenType == "Aggregate") {
            columns.push({
              token: t.parent.fullKey,
              summaryToken: t.fullKey
            });
          } else {
            columns.push({
              token: t.fullKey,
            });
          }
        }
      });

      window.open(Finder.findOptionsPath({
        queryName: lcr.queryKey,
        filterOptions: toFilterOptions(filters),
        columnOptions: columns,
        columnOptionsMode: "ReplaceOrAdd"
      }));
    }
  }

  function orderClassName(column: ColumnOptionParsed) {
    if (column.token == undefined)
      return "";

    const columns = p.chartRequest.columns;

    const c = columns.filter(a => a.element.token != null && a.element.token!.token!.fullKey == column.token!.fullKey).firstOrNull();
    if (c == undefined || c.element.orderByType == null)
      return "";

    return (c.element.orderByType == "Ascending" ? "asc" : "desc") + (" l" + c.element.orderByIndex);
  }
  const resultTable = p.resultTable;

  const chartRequest = p.chartRequest;

  const qs = Finder.getSettings(chartRequest.queryKey);

  const columns = chartRequest.columns.map(c => c.element).filter(cc => cc.token != undefined)
    .map(cc => ({ token: cc.token!.token, displayName: cc.displayName } as ColumnOptionParsed))
    .map(co => ({
      column: co,
      cellFormatter: (qs?.formatters && qs.formatters[co.token!.fullKey]) ?? Finder.formatRules.filter(a => a.isApplicable(co.token!, undefined)).last("FormatRules").formatter(co.token!, undefined),
      resultIndex: resultTable.columns.indexOf(co.token!.fullKey)
    }));

  var hasEntity = !ChartClient.hasAggregates(chartRequest);

  var entityFormatter = qs?.entityFormatter || Finder.entityFormatRules.filter(a => a.isApplicable(undefined)).last("EntityFormatRules").formatter;

  return (
    <div className="sf-scroll-table-container">
      <FullscreenComponent onReload={p.onReload}>
        <table className="sf-search-results table table-hover table-sm">
          <thead>
            <tr>
              {hasEntity && <th></th>}
              {columns.map((col, i) =>
                <th key={i} data-column-name={col.column.token!.fullKey}
                  onClick={e => handleHeaderClick(e, col.column)}>
                  <span className={"sf-header-sort " + orderClassName(col.column)} />
                  <span> {col.column.displayName || col.column.token!.niceName}</span>
                </th>)}
            </tr>
          </thead>
          <tbody>
            {
              resultTable.rows.map((row, i) => {
                const ctx: Finder.CellFormatterContext = {
                  refresh: undefined,
                  columns: resultTable.columns,
                  row: row,
                  rowIndex: i,
                }
                return (
                  <tr key={i} onDoubleClick={e => handleOnDoubleClick(e, row)}>
                    {hasEntity &&
                      <td className={entityFormatter.cellClass}>
                        {entityFormatter.formatter(row, resultTable.columns, undefined)}
                      </td>
                    }
                    {columns.map((c, j) =>
                      <td key={j} className={c.cellFormatter && c.cellFormatter.cellClass}>
                        {c.resultIndex == -1 || c.cellFormatter == undefined ? undefined : c.cellFormatter.formatter(row.columns[c.resultIndex], ctx, c.column.token!)}
                      </td>)
                    }
                  </tr>
                );
              })
            }
          </tbody>
        </table>
      </FullscreenComponent>
    </div>
  );
}




