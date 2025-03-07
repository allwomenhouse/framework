import * as React from 'react'
import { classes } from '../Globals'
import * as Finder from '../Finder'
import * as Constructor from '../Constructor'
import { FindOptions, FindOptionsParsed, QueryDescription, QueryToken, QueryValueRequest } from '../FindOptions'
import { Lite, Entity, isEntity, EntityControlMessage, isLite } from '../Signum.Entities'
import { getQueryKey, getQueryNiceName, QueryTokenString, tryGetTypeInfos, getTypeInfos } from '../Reflection'
import * as Navigator from '../Navigator'
import { StyleContext, TypeContext } from '../TypeContext'
import SearchValue, { SearchValueController } from './SearchValue'
import { FormGroup } from '../Lines/FormGroup'
import { SearchControlProps } from "./SearchControl";
import { BsColor, BsSize } from '../Components';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { EntityBaseController } from '../Lines/EntityBase'
import SelectorModal from '../SelectorModal'
import { useForceUpdate } from '../Hooks'

export interface SearchValueLineProps {
  ctx: StyleContext;
  findOptions?: FindOptions;
  valueToken?: string | QueryTokenString<any>;
  multipleValues?: { vertical?: boolean, showType?: boolean };
  label?: React.ReactChild | (() => React.ReactChild);
  labelHtmlAttributes?: React.HTMLAttributes<HTMLLabelElement>;
  unit?: React.ReactChild;
  format?: string;
  formGroupHtmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  helpText?: (vscc: SearchValueController) => React.ReactChild | undefined;
  initialValue?: any;
  isLink?: boolean;
  isBadge?: boolean | "MoreThanZero";
  badgeColor?: BsColor;
  customClass?: string | ((value: any | undefined) => (string | undefined));
  customStyle?: React.CSSProperties;
  isFormControl?: boolean;
  findButton?: boolean;
  viewEntityButton?: boolean;
  avoidAutoRefresh?: boolean;
  deps?: React.DependencyList;
  extraButtons?: (vscc: SearchValueController) => React.ReactNode;
  create?: boolean;
  onCreate?: () => Promise<any>;
  getViewPromise?: (e: any /*Entity*/) => undefined | string | Navigator.ViewPromise<any /*Entity*/>;
  searchControlProps?: Partial<SearchControlProps>;
  modalSize?: BsSize;
  onExplored?: () => void;
  onViewEntity?: (entity: Lite<Entity>) => void;
  onValueChanged?: (value: any) => void;
  customRequest?: (req: QueryValueRequest, fop: FindOptionsParsed, token: QueryToken | null, signal: AbortSignal) => Promise<any>,
}

export interface SearchValueLineController {
  searchValue: SearchValueController | null | undefined;
  refreshValue(): void;
}

const SearchValueLine = React.forwardRef(function SearchValueLine(p: SearchValueLineProps, ref?: React.Ref<SearchValueLineController>) {

  var svRef = React.useRef<SearchValueController | null>();

  React.useImperativeHandle(ref, () => ({
    searchValue: svRef.current,
    refreshValue: () => svRef.current?.refreshValue(),
  }), [svRef.current]);


  const forceUpdate = useForceUpdate();

  var handleSearchValueLoaded = React.useCallback((vsc: SearchValueController | null) => {
    if (vsc != svRef.current) {
      forceUpdate();
    }
    svRef.current = vsc;
  }, []);

  function getFindOptions(props: SearchValueLineProps): FindOptions {
    if (props.findOptions)
      return props.findOptions;

    var ctx = props.ctx as TypeContext<any>;

    if (isEntity(ctx.value))
      return {
        queryName: ctx.value.Type,
        filterOptions: [{ token: QueryTokenString.entity(), value: ctx.value }]
      };

    if (isLite(ctx.value))
      return {
        queryName: ctx.value.EntityType,
        filterOptions: [{ token: QueryTokenString.entity(), value: ctx.value }]
      };

    throw new Error("Impossible to determine 'findOptions' because 'ctx' is not a 'TypeContext<Entity>'. Set it explicitly");
  }

  var fo = getFindOptions(p);

  if (!Finder.isFindable(fo.queryName, false))
    return null;

  var errorMessage = Finder.validateNewEntities(fo);
  if (errorMessage) {
    return (
      <div className="alert alert-danger" role="alert">
        <strong>Error in SearchValueLine ({getQueryKey(fo.queryName)}): </strong>
        {errorMessage}
      </div>
    );
  }


  var token = svRef.current?.valueToken;

  const isQuery = p.valueToken == undefined || token?.queryTokenType == "Aggregate";

  const isBadge = (p.isBadge ?? p.valueToken == undefined ? "MoreThanZero" as "MoreThanZero" : false);
  const isFormControl = (p.isFormControl ?? p.valueToken != undefined);

  const unit = isFormControl && (p.unit ?? token?.unit) && <span className="input-group-text">{p.unit ?? token?.unit}</span>;

  const ctx = p.ctx;

  const value = svRef.current?.value;
  const find = value != undefined && (p.findButton ?? isQuery) &&
    <a href="#" className={classes("sf-line-button sf-find", isFormControl ? "btn input-group-text" : undefined)}
      onClick={svRef.current!.handleClick}
      title={ctx.titleLabels ? EntityControlMessage.Find.niceToString() : undefined}>
      {EntityBaseController.findIcon}
    </a>;

  const create = (p.create ?? false) &&
    <a href="#" className={classes("sf-line-button sf-create", isFormControl ? "btn input-group-text" : undefined)}
      onClick={handleCreateClick}
      title={ctx.titleLabels ? EntityControlMessage.Create.niceToString() : undefined}>
      {EntityBaseController.createIcon}
    </a>;

  const view = value != undefined && (p.viewEntityButton ?? (isLite(value) && Navigator.isViewable(value.EntityType))) &&
    <a href="#" className={classes("sf-line-button sf-view", isFormControl ? "btn input-group-text" : undefined)}
      onClick={handleViewEntityClick}
      title={ctx.titleLabels ? EntityControlMessage.View.niceToString() : undefined}>
      {EntityBaseController.viewIcon}
    </a>

  let extra = svRef.current && p.extraButtons && p.extraButtons(svRef.current);

  var label = p.label == undefined ? undefined :
    typeof p.label == "function" ? p.label() :
      p.label;

  return (
    <FormGroup ctx={p.ctx}
      label={label ?? token?.niceName ?? getQueryNiceName(fo.queryName)}
      labelHtmlAttributes={p.labelHtmlAttributes}
      htmlAttributes={{ ...p.formGroupHtmlAttributes, ...{ "data-value-query-key": getQueryKey(fo.queryName) } }}
      helpText={p.helpText && svRef.current && p.helpText(svRef.current)}
    >
      <div className={isFormControl ? ((unit || view || extra || find || create) ? p.ctx.inputGroupClass : undefined) : p.ctx.formControlPlainTextClass}>
        <SearchValue
          ref={handleSearchValueLoaded}
          findOptions={fo}
          format={p.format}
          initialValue={p.initialValue}
          onInitialValueLoaded={() => forceUpdate()}
          multipleValues={p.multipleValues}
          isBadge={isBadge}
          customClass={p.customClass ?? (p.multipleValues ? p.ctx.labelClass : undefined)}
          customStyle={p.customStyle}
          badgeColor={p.badgeColor}
          isLink={p.isLink ?? Boolean(p.multipleValues)}
          formControlClass={isFormControl && !p.multipleValues ? p.ctx.formControlClass + " readonly" : undefined}
          valueToken={p.valueToken}
          onValueChange={v => { forceUpdate(); p.onValueChanged && p.onValueChanged(v); }}
          onTokenLoaded={() => forceUpdate()}
          onExplored={p.onExplored}
          searchControlProps={p.searchControlProps}
          modalSize={p.modalSize}
          deps={p.deps}
          customRequest={p.customRequest}
        />

        {unit}
        {view}
        {find}
        {create}
        {extra}
      </div>
    </FormGroup>
  );


  function handleViewEntityClick(e: React.MouseEvent<any>) {
    e.preventDefault();

    var entity = svRef.current!.value as Lite<Entity>;
    if (p.onViewEntity)
      p.onViewEntity(entity);

    Navigator.view(entity)
      .then(() => {
        svRef.current!.refreshValue();
        p.onExplored && p.onExplored();
      });
  }

  function handleCreateClick(e: React.MouseEvent<any>) {
    e.preventDefault();

    if (p.onCreate) {
      p.onCreate().then(() => {
        if (!p.avoidAutoRefresh)
          svRef.current!.refreshValue();
      });
    } else {

      var fo = p.findOptions!;
      const isWindowsOpen = e.button == 1 || e.ctrlKey;
      Finder.getQueryDescription(fo.queryName).then(qd => {
        chooseType(qd).then(tn => {
          if (tn == null)
            return;

          var s = Navigator.getSettings(tn);
          var qs = Finder.getSettings(fo.queryName);
          var getViewPromise = p.getViewPromise ?? qs?.getViewPromise;

          if (isWindowsOpen || (s != null && s.avoidPopup)) {
            var vp = getViewPromise && getViewPromise(null)

            window.open(Navigator.createRoute(tn, vp && typeof vp == "string" ? vp : undefined));
          } else {
            Finder.parseFilterOptions(fo.filterOptions || [], false, qd)
              .then(fos => Finder.getPropsFromFilters(tn, fos)
                .then(props => Constructor.constructPack(tn, props))
                .then(pack => pack && Navigator.view(pack!, {
                  getViewPromise: getViewPromise as any,
                  createNew: () => Finder.getPropsFromFilters(tn, fos)
                    .then(props => Constructor.constructPack(tn, props)!),
                })))
              .then(() => p.avoidAutoRefresh ? undefined : svRef.current!.refreshValue());
          }
        });
      });
    }
  }

  function chooseType(qd: QueryDescription): Promise<string | undefined> {

    const tis = getTypeInfos(qd.columns["Entity"].type)
      .filter(ti => Navigator.isCreable(ti, { isSearch: true }));

    return SelectorModal.chooseType(tis)
      .then(ti => ti ? ti.name : undefined);
  }

});


export default SearchValueLine; 
