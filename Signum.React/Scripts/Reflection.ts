import { DateTime, DateTimeFormatOptions, Duration, DurationObjectUnits, Settings } from 'luxon';
import { Dic } from './Globals';
import type { ModifiableEntity, Entity, Lite, MListElement, ModelState, MixinEntity, OperationSymbol, ModelEntity } from './Signum.Entities'; //ONLY TYPES or Cyclic problems in Webpack!
import { ajaxGet, ThrowErrorFilter } from './Services';
import { MList } from "./Signum.Entities";
import * as AppContext from './AppContext';
import { QueryString } from './QueryString';
import { func } from 'prop-types';

export function getEnumInfo(enumTypeName: string, enumId: number): MemberInfo {

  const ti = tryGetTypeInfo(enumTypeName);

  if (!ti || ti.kind != "Enum")
    throw new Error(`${enumTypeName} is not an Enum`);

  return ti.membersById![enumId];
}

export interface TypeInfo {
  kind: KindOfType;
  name: string;
  fullName: string;
  niceName?: string;
  nicePluralName?: string;
  gender?: string;
  entityKind?: EntityKind;
  entityData?: EntityData;
  toStringFunction?: string;
  customLiteModels?: { [modelType: string]: CustomLiteModel };
  isLowPopulation?: boolean;
  isSystemVersioned?: boolean;
  requiresSaveOperation?: boolean;
  queryDefined?: boolean;
  requiresEntityPack?: boolean;
  members: { [name: string]: MemberInfo };
  membersById?: { [name: string]: MemberInfo };
  hasConstructorOperation?: boolean;
  operations?: { [name: string]: OperationInfo };
}

export interface CustomLiteModel {
  isDefault: boolean;
  constructorFunctionString?: string;
  constructorFunction?: (e: Entity) => ModelEntity;
} 

export interface MemberInfo {
  name: string,
  niceName: string;
  type: TypeReference;
  isReadOnly?: boolean;
  isIgnoredEnum?: boolean;
  unit?: string;
  format?: string;
  required?: boolean;
  maxLength?: number;
  isMultiline?: boolean;
  isVirtualMList?: boolean;
  preserveOrder?: boolean;
  avoidDuplicates?: boolean;
  notVisible?: boolean;
  id?: any; //symbols
}

export interface OperationInfo {
  key: string,
  niceName: string;
  operationType: OperationType;
  canBeNew?: boolean;
  canBeModified?: boolean;
  hasCanExecute?: boolean;
  hasStates?: boolean;
}

export type OperationType =
  "Execute" |
  "Delete" |
  "Constructor" |
  "ConstructorFrom" |
  "ConstructorFromMany";

//https://moment.github.io/luxon/docs/manual/formatting.html#formatting-with-tokens--strings-for-cthulhu-
//https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings
export function toLuxonFormat(netFormat: string | undefined, type: "DateOnly" | "DateTime"): string {

  if (!netFormat)
    return type == "DateOnly" ? "D" : "F";
  
  switch (netFormat) {
    case "d": return "D"; // toFormatWithFixes
    case "D": return "DDDD";
    case "f": return "fff"
    case "F": return "FFF";
    case "g": return "f";
    case "G": return "F";
    case "M":
    case "m": return "dd LLLL";
    case "u": return "yyyy-MM-dd'T'HH:mm:ss";
    case "s": return "yyyy-MM-dd'T'HH:mm:ss";
    case "o": return "yyyy-MM-dd'T'HH:mm:ss.u";
    case "t": return "t";
    case "T": return "tt";
    case "y": return "LLLL yyyy";
    case "Y": return "LLLL yyyy";
    default: {
      const result = netFormat
        .replaceAll("f", "S")
        .replaceAll("tt", "A")
        .replaceAll("t", "a")
        .replaceAll("YYYY", "yyyy")
        .replaceAll("YY", "yy")
        .replaceAll("dddd", "cccc")
        .replaceAll("ddd", "ccc")
        .replaceAll("ddd", "ccc")
        .replaceAll("T", "'T'");
      return result;
    }
  }
}

export function splitLuxonFormat(luxonFormat: string) : [dateFormat: string, durationFormat: string | null] {

  switch (luxonFormat) {
    case "D": return ["D", null];
    case "DD": return ["DD", null];
    case "DDD": return ["DDD", null];
    case "DDDD": return ["DDDD", null];
    case "F": return ["D", "hh:mm:ss"];
    case "FF": return ["DD", "hh:mm:ss"];
    case "FFF": return ["DDD", "hh:mm:ss"];
    case "FFFF": return ["DDDD", "hh:mm:ss"];
    case "f": return ["D", "hh:mm"];
    case "ff": return ["DD", "hh:mm"];
    case "fff": return ["DDD", "hh:mm"];
    case "ffff": return ["DDDD", "hh:mm"];
  }

  if (luxonFormat.contains("'T'"))
    return [luxonFormat.before("'T'"), luxonFormat.after("'T'").replaceAll("H", "h")];

  if (luxonFormat.contains(" ") && !luxonFormat.after(" ").contains(" "))
    return [luxonFormat.before(" "), luxonFormat.after(" ").replaceAll("H", "h")];

  throw new Error("Unable to split " + luxonFormat);
}

export function dateTimePlaceholder(luxonFormat: string) {
  var result = DateTime.expandFormat(luxonFormat);

  return result
    .replace(/\bd\b/, "dd")
    .replace(/\bMM?\b/, "mm")
    .replace(/\bh\b/, "hh")
    .replace(/\bHH?\b/, "hh")
    .replace(/\bm\b/, "mm");

}

export function timePlaceholder(durationFormat: string) {
  return durationFormat;
}

const oneDigitCulture = new Set([
  "agq", "agq-CM",
  "ar-001", "ar-DJ", "ar-ER", "ar-IL", "ar-KM", "ar-MR", "ar-PS", "ar-SD", "ar-SO", "ar-SS", "ar-TD",
  "ast", "ast-ES",
  "bas", "bas-CM",
  "bg", "bg-BG",
  "bin", "bin-NG",
  "bm", "bm-Latn", "bm-Latn-ML",
  "bn", "bn-BD",
  "bo", "bo-CN",
  "brx", "brx-IN",
  "bs", "bs-Cyrl", "bs-Cyrl-BA", "bs-Latn", "bs-Latn-BA",
  "ca", "ca-AD", "ca-ES", "ca-ES-valencia", "ca-FR", "ca-IT",
  "ccp", "ccp-Cakm", "ccp-Cakm-BD", "ccp-Cakm-IN",
  "ceb", "ceb-Latn", "ceb-Latn-PH",
  "chr", "chr-Cher", "chr-Cher-US",
  "dje", "dje-NE",
  "dsb", "dsb-DE",
  "dua", "dua-CM",
  "dyo", "dyo-SN",
  "ee", "ee-GH", "ee-TG",
  "el", "el-CY", "el-GR",
  "en", "en-AS", "en-AU", "en-BI", "en-GU", "en-HK", "en-JM", "en-MH", "en-MP", "en-MY", "en-NZ", "en-PR", "en-SG", "en-UM", "en-US", "en-VI", "en-ZW",
  "es-419", "es-AR", "es-BO", "es-BR", "es-BZ", "es-CO", "es-CR", "es-CU", "es-DO", "es-EC", "es-GQ", "es-GT", "es-HN", "es-NI", "es-PE", "es-PH", "es-PY", "es-SV", "es-US", "es-UY", "es-VE",
  "eu", "eu-ES",
  "ewo", "ewo-CM",
  "ff-Latn-BF", "ff-Latn-CM", "ff-Latn-GH", "ff-Latn-GM", "ff-Latn-GN", "ff-Latn-GW", "ff-Latn-LR", "ff-Latn-MR", "ff-Latn-NE", "ff-Latn-NG", "ff-Latn-SL",
  "fi", "fi-FI",
  "fil", "fil-PH",
  "ha", "ha-Latn", "ha-Latn-GH", "ha-Latn-NE", "ha-Latn-NG",
  "haw", "haw-US",
  "hr", "hr-BA", "hr-HR",
  "hsb", "hsb-DE",
  "ibb", "ibb-NG",
  "ig", "ig-NG",
  "ii", "ii-CN",
  "is", "is-IS",
  "iu", "iu-Cans", "iu-Cans-CA", "iu-Latn", "iu-Latn-CA",
  "kab", "kab-DZ",
  "kea", "kea-CV",
  "khq", "khq-ML",
  "ko-KP",
  "kr", "kr-Latn", "kr-Latn-NG",
  "ks", "ks-Arab", "ks-Arab-IN",
  "ksf", "ksf-CM",
  "ksh", "ksh-DE",
  "ky", "ky-KG",
  "lkt", "lkt-US",
  "ln", "ln-AO", "ln-CD", "ln-CF", "ln-CG",
  "lo", "lo-LA",
  "lu", "lu-CD",
  "mfe", "mfe-MU",
  "ml", "ml-IN",
  "mn-Mong", "mn-Mong-CN", "mn-Mong-MN",
  "moh", "moh-CA",
  "ms", "ms-BN", "ms-MY", "ms-SG",
  "mua", "mua-CM",
  "nds", "nds-DE", "nds-NL",
  "ne", "ne-IN", "ne-NP",
  "nl", "nl-BE", "nl-NL",
  "nmg", "nmg-CM",
  "nus", "nus-SS",
  "pap", "pap-029",
  "prs", "prs-AF",
  "ps", "ps-AF", "ps-PK",
  "rn", "rn-BI",
  "se-FI",
  "seh", "seh-MZ",
  "ses", "ses-ML",
  "sg", "sg-CF",
  "shi", "shi-Latn", "shi-Latn-MA", "shi-Tfng", "shi-Tfng-MA",
  "sk", "sk-SK",
  "sl", "sl-SI",
  "smn", "smn-FI",
  "sms", "sms-FI",
  "sq", "sq-AL", "sq-MK", "sq-XK",
  "sr", "sr-Cyrl-BA", "sr-Cyrl-ME", "sr-Cyrl-XK", "sr-Latn", "sr-Latn-BA", "sr-Latn-ME", "sr-Latn-RS", "sr-Latn-XK",
  "ta-LK", "ta-MY", "ta-SG",
  "th", "th-TH",
  "to", "to-TO",
  "tr", "tr-CY", "tr-TR",
  "twq", "twq-NE",
  "tzm-Arab", "tzm-Arab-MA",
  "ug", "ug-CN",
  "ur-IN",
  "yav", "yav-CM",
  "yo", "yo-BJ", "yo-NG",
  "zgh", "zgh-Tfng", "zgh-Tfng-MA",
  "zh", "zh-CN", "zh-Hans", "zh-Hans-HK", "zh-Hans-MO", "zh-Hant", "zh-HK", "zh-MO", "zh-SG", "zh-TW",
  "zu", "zu-ZA"
]);

export function toFormatWithFixes(dt: DateTime, format: string, options ?: Intl.DateTimeFormatOptions){

  if (!oneDigitCulture.has(dt.locale)) {

    if (format == "D")
      return dt.toLocaleString({ year: "numeric", month: "2-digit", day: "2-digit", ...options });

    if (format == "f")
      return dt.toLocaleString({ year: "numeric", month: "2-digit", day: "2-digit", hour: "numeric", minute: "numeric", ...options });

    if (format == "F")
      return dt.toLocaleString({ year: "numeric", month: "2-digit", day: "2-digit", hour: "numeric", minute: "numeric", second: "numeric", ...options });
  }

  if (format == "EE") //missing
    return dt.toFormat("EEE", options).substr(0, 2);

  return dt.toFormat(format, options)

}


//https://msdn.microsoft.com/en-us/library/ee372286(v=vs.110).aspx
//https://github.com/jsmreese/moment-duration-format
export function toLuxonDurationFormat(netFormat: string | undefined): string | undefined {

  if (netFormat == undefined)
    return undefined;

  return netFormat.replaceAll("\\:", ":").replaceAll("H", "h");
}

export namespace NumberFormatSettings {
  export let defaultNumberFormatLocale: string = null!;
}

//https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings
export function toNumberFormat(format: string | undefined, locale?: string): Intl.NumberFormat {
    let loc = locale ?? NumberFormatSettings.defaultNumberFormatLocale;
    if (loc.startsWith("es-")) {
        loc = "de-DE"; //fix problem for Intl formatting "es" numbers for 4 digits over decimal point 
    }
  return new Intl.NumberFormat(loc, toNumberFormatOptions(format));
}

export function toNumberFormatOptions(format: string | undefined): Intl.NumberFormatOptions | undefined {

  if (format == undefined)
    return undefined;

  const f = format.toUpperCase();

  function parseIntDefault(str: string, defaultValue: number) {
    var result = parseInt(str);
    if (isNaN(result))
      return defaultValue;

    return result;
  }

  if (f.startsWith("C")) //unit comes separated
    return {
      style: "decimal",
      minimumFractionDigits: parseIntDefault(f.after("C"), 2),
      maximumFractionDigits: parseIntDefault(f.after("C"), 2),
      useGrouping: true,
    }

  if (f.startsWith("N"))
    return {
      style: "decimal",
      minimumFractionDigits: parseIntDefault(f.after("N"), 2),
      maximumFractionDigits: parseIntDefault(f.after("N"), 2),
      useGrouping: true,
    }

  if (f.startsWith("D"))
    return {
      style: "decimal",
      maximumFractionDigits: 0,
      minimumIntegerDigits: parseIntDefault(f.after("D"), 1),
      useGrouping: false,
    }

  if (f.startsWith("F"))
    return {
      style: "decimal",
      minimumFractionDigits: parseIntDefault(f.after("F"), 2),
      maximumFractionDigits: parseIntDefault(f.after("F"), 2),
      useGrouping: false,
    }

  if (f.startsWith("E"))
    return {
      style: "decimal",
      notation: "scientific",
      minimumFractionDigits: parseIntDefault(f.after("E"), 6),
      maximumFractionDigits: parseIntDefault(f.after("E"), 6),
      useGrouping: false,
    } as any;

  if (f.startsWith("P")) {
    return {
      style: "percent",
      minimumFractionDigits: parseIntDefault(f.after("P"), 2),
      maximumFractionDigits: parseIntDefault(f.after("P"), 2),
      useGrouping: false,
    }
  }

  //simple heuristic

  var regex = /(?<plus>\+)?(?<body>[0#,.]+)(?<suffix>[%MKB])?/

  const match = regex.exec(f);

  var body = match?.groups?.body ?? f;
  const suffix = match?.groups?.suffix;

  var afterDot = body.tryAfter(".") ?? "";
  const result: Intl.NumberFormatOptions = {
    style: suffix == "%" ? "percent" : "decimal",
    notation: suffix == "K" || suffix == "M" || suffix == "B" ? "compact": undefined,
    minimumFractionDigits: afterDot.replaceAll("#", "").length,
    maximumFractionDigits: afterDot.length,
    useGrouping: f.contains(","),
  };

  if (match?.groups?.plus)
    (result as any).signDisplay = "always";

  return result;
}

export function valToString(val: any) {
  if (val == null)
    return "";

  return val.toString();
}

export function numberToString(val: any, format?: string) {
  if (val == null)
    return "";

  return toNumberFormat(format).format(val);
}

export function dateToString(val: any, type: "DateOnly" | "DateTime", netFormat?: string) {
  if (val == null)
    return "";

  var m = DateTime.fromISO(val);
  return m.toFormat(toLuxonFormat(netFormat, type));
}

export function timeToString(val: any, netFormat?: string) {
  if (val == null)
    return "";

  var duration = Duration.fromISOTime(val);
  return duration.toFormat(toLuxonDurationFormat(netFormat) ?? "HH:mm:ss");
}


export interface TypeReference {
  name: string;
  typeNiceName?: string;
  isCollection?: boolean;
  isLite?: boolean;
  isNotNullable?: boolean;
  isEmbedded?: boolean;
}

export type KindOfType = "Entity" | "Enum" | "Message" | "Query" | "SymbolContainer";

export type EntityKind = "SystemString" | "System" | "Relational" | "String" | "Shared" | "Main" | "Part" | "SharedPart";
export const EntityKindValues: EntityKind[] = ["SystemString", "System", "Relational", "String", "Shared", "Main", "Part", "SharedPart"];

export type EntityData = "Master" | "Transactional";
export const EntityDataValues: EntityData[] = ["Master", "Transactional"];

export function getAllTypes(): TypeInfo[] {
  return Dic.getValues(_types);
}

export interface TypeInfoDictionary {
  [name: string]: TypeInfo
}

let _types: TypeInfoDictionary = {};


let _queryNames: {
  [queryKey: string]: MemberInfo
};

export type PseudoType = IType | TypeInfo | string;

export function getTypeName(pseudoType: IType | TypeInfo | string | Lite<Entity> | ModifiableEntity): string {
  if ((pseudoType as Lite<Entity>).EntityType)
    return (pseudoType as Lite<Entity>).EntityType;

  if ((pseudoType as ModifiableEntity).Type)
    return (pseudoType as ModifiableEntity).Type;

  if ((pseudoType as IType).typeName)
    return (pseudoType as IType).typeName;

  if ((pseudoType as TypeInfo).name)
    return (pseudoType as TypeInfo).name;

  if (typeof pseudoType == "string")
    return pseudoType as string;

  throw new Error("Unexpected pseudoType " + pseudoType);
}

export function isTypeEntity(type: PseudoType): boolean {
  const ti = tryGetTypeInfo(type);
  return ti != null && ti.kind == "Entity" && !!ti.members["Id"];
}

export function isTypeEnum(type: PseudoType): boolean {
  const ti = tryGetTypeInfo(type);
  return ti != null && ti.kind == "Enum";
}

export function isTypeModel(type: PseudoType): boolean {
  const ti = tryGetTypeInfo(type);
  return ti != null && ti.kind == "Entity" && !ti.members["Id"];
}

export function isTypeModifiableEntity(type: TypeReference): boolean {
  return type.isEmbedded == true || tryGetTypeInfos(type).every(ti => ti != undefined && (isTypeEntity(ti) || isTypeModel(ti)));
}

export function getTypeInfo(type: PseudoType): TypeInfo {

  const typeName = getTypeName(type);

  const ti = _types[typeName.toLowerCase()];

  if (ti == null)
    throw new Error(`Type not found: ${typeName}`);

  return ti;
}

export function tryGetTypeInfo(type: PseudoType): TypeInfo | undefined {

  const typeName = getTypeName(type);

  if (typeName == null)
    throw new Error("Unexpected type: " + type);

  const ti: TypeInfo | undefined = _types[typeName.toLowerCase()];

  return ti;
}

export function isLowPopulationSymbol(type: PseudoType) {

  var ti = tryGetTypeInfo(type);

  return ti != null && ti.kind == "Entity" && ti.fullName.endsWith("Symbol") && ti.isLowPopulation;
}

export function parseId(ti: TypeInfo, id: string): string | number {
  return ti.members["Id"].type.name == "number" ? parseInt(id) : id;
}

export const IsByAll = "[ALL]";
export function getTypeInfos(typeReference: TypeReference | string): TypeInfo[] {

  const name = typeof typeReference == "string" ? typeReference : typeReference.name;

  if (name == IsByAll || name == "")
    return [];

  return name.split(", ").map(getTypeInfo);
}

export function tryGetTypeInfos(typeReference: TypeReference | string): (TypeInfo | undefined)[] {

  const name = typeof typeReference == "string" ? typeReference : typeReference.name;

  if (name == IsByAll || name == "")
    return [];

  return name.split(", ").map(tryGetTypeInfo);
}

export function getQueryNiceName(queryName: PseudoType | QueryKey): string {

  if ((queryName as TypeInfo).kind != undefined)
    return (queryName as TypeInfo).nicePluralName!;

  if (queryName instanceof Type)
    return (queryName as Type<any>).nicePluralName();

  if (queryName instanceof QueryKey)
    return (queryName as QueryKey).niceName();

  if (typeof queryName == "string") {
    const str = queryName as string;

    const type = _types[str.toLowerCase()];
    if (type)
      return type.nicePluralName!;

    const qn = _queryNames[str.toLowerCase()];
    if (qn)
      return qn.niceName;

    return str;
  }

  throw new Error("unexpected queryName type");

}

export function getQueryInfo(queryName: PseudoType | QueryKey): MemberInfo | TypeInfo {
  if (queryName instanceof QueryKey) {
    return queryName.memberInfo();
  }
  else {
    const ti = tryGetTypeInfo(queryName);
    if (ti)
      return ti;

    const mi = _queryNames[(queryName as string).toLowerCase()];

    if (mi)
      return mi;

    throw Error("Unexpected query type");
  }
}

export function getQueryKey(queryName: PseudoType | QueryKey): string {
  if ((queryName as TypeInfo).kind != undefined)
    return (queryName as TypeInfo).name;

  if (queryName instanceof Type)
    return (queryName as Type<any>).typeName;

  if (queryName instanceof QueryKey)
    return (queryName as QueryKey).name;

  if (typeof queryName == "string") {
    const str = queryName as string;

    const type = _types[str.toLowerCase()];
    if (type)
      return type.name;

    const qn = _queryNames[str.toLowerCase()];
    if (qn)
      return qn.name;

    return str;
  }

  throw Error("Unexpected query type");
}

export function isQueryDefined(queryName: PseudoType | QueryKey): boolean {
  if ((queryName as TypeInfo).kind != undefined)
    return (queryName as TypeInfo).queryDefined || false;

  if (queryName instanceof Type) {
    var ti = tryGetTypeInfo(queryName)
    return ti && ti.queryDefined || false;
  }

  if (queryName instanceof QueryKey)
    return !!_queryNames[queryName.name.toLowerCase()];

  if (typeof queryName == "string") {
    const str = queryName as string;

    const type = _types[str.toLowerCase()];
    if (type) {
      return type.queryDefined || false;
    }

    const qn = _queryNames[str.toLowerCase()];

    return !!qn;
  }

  return false;
}

export function reloadTypes(): Promise<void> {
  return ajaxGet<TypeInfoDictionary>({
    url: "~/api/reflection/types?" + QueryString.stringify({
      user: AppContext.currentUser?.id,
      userTicks: AppContext.currentUser?.ticks,
      culture: AppContext.currentCulture
    })
  })
    .then(types => {
      setTypes(types);
      onReloadTypes();
    });
}

export const onReloadTypesActions: Array<() => void> = [];

export function onReloadTypes() {
  onReloadTypesActions.forEach(a => a());
}


export function setTypes(types: TypeInfoDictionary) {

  Dic.foreach(types, (k, t) => {
    t.name = k;
    if (t.members) {
      Dic.foreach(t.members, (k2, t2) => t2.name = k2);
      Object.freeze(t.members);

      if (t.kind == "Enum") {
        t.membersById = Dic.getValues(t.members).toObject(a => a.name);
        Object.freeze(t.membersById);
      }
    }

    if (t.requiresSaveOperation == undefined && t.entityKind)
      t.requiresSaveOperation = calculateRequiresSaveOperation(t.entityKind);

    Object.freeze(t);
  });

  _types = Dic.getValues(types).toObject(a => a.name.toLowerCase());
  Object.freeze(_types);

  Dic.foreach(types, (k, t) => {
    if (t.operations) {
      Dic.foreach(t.operations, (k2, t2) => {
        t2.key = k2;
        const typeName = k2.before(".").toLowerCase();
        const memberName = k2.after(".");

        const ti = _types[typeName];
        if (!ti)
          console.error(`Type ${typeName} not found (looking for operatons of ${t.name}). Consider synchronizing of calling ReflectionServer.RegisterLike.`);
        else {
          const member = ti.members[k2.after(".")];
          if (!member)
            console.error(`Member ${memberName} not found in ${ti.name} (looking for operatons of ${t.name}). Consider synchronizing of calling ReflectionServer.RegisterLike.`);
          else
            t2.niceName = member.niceName;
        }
      });

      Object.freeze(t.operations);
    }
  });

  _queryNames = Dic.getValues(types).filter(t => t.kind == "Query")
    .flatMap(a => Dic.getValues(a.members))
    .toObject(m => m.name.toLowerCase(), m => m);

  Object.freeze(_queryNames);

  missingSymbols = missingSymbols.filter(s => {
    const m = getMember(s.key);
    if (m)
      s.id = m.id;

    return s.id == null;
  });
}

function calculateRequiresSaveOperation(entityKind: EntityKind): boolean {
  switch (entityKind) {
    case "SystemString": return false;
    case "System": return false;
    case "Relational": return false;
    case "String": return true;
    case "Shared": return true;
    case "Main": return true;
    case "Part": return false;
    case "SharedPart": return false;
    default: throw new Error("Unexpeced entityKind");
  }
}

export interface IBinding<T> {
  getValue(): T;
  setValue(val: T): void;
  suffix: string;
  getIsReadonly(): boolean;
  getError(): string | undefined;
  setError(value: string | undefined): void;
}

export class Binding<T> implements IBinding<T> {

  initialValue: T; // For deep compare
  suffix: string;
  constructor(
    public parentObject: any,
    public member: string | number,
    suffix?: string) {
    this.initialValue = this.parentObject[member];
    this.suffix = suffix || ("." + member);
  }

  static create<F, T>(parentValue: F, fieldAccessor: (from: F) => T) {

    const memberName = Binding.getSingleMember(fieldAccessor);

    return new Binding<T>(parentValue, memberName, "." + memberName);
  }

  static getSingleMember(fieldAccessor: (from: any) => any) {
    const members = getLambdaMembers(fieldAccessor);

    if (members.length != 1 || members[0].type != "Member")
      throw Error("invalid function 'fieldAccessor'");

    return members[0].name;
  }

  getValue(): T {

    if (!this.parentObject)
      throw new Error(`Impossible to get '${this.member}' from '${this.parentObject}'`);

    return this.parentObject[this.member];
  }

  setValue(val: T) {

    if (!this.parentObject)
      throw new Error(`Impossible to set '${this.member}' from '${this.parentObject}'`);

    const oldVal = this.parentObject[this.member];
    this.parentObject[this.member] = val;

    if ((this.parentObject as ModifiableEntity).Type) {
      if (oldVal !== val && !sameEntity(oldVal, val) || Array.isArray(oldVal)) {
        (this.parentObject as ModifiableEntity).modified = true;
      }
    }

    this.initialValue = val;
  }

  deleteValue() {
    if (!this.parentObject)
      throw new Error(`Impossible to delete '${this.member}' from '${this.parentObject}'`);

    delete this.parentObject[this.member];
  }

  forceError: string | undefined;

  getError(): string | undefined {

    if (this.forceError)
      return this.forceError;

    const parentErrors = (this.parentObject as ModifiableEntity).error;
    return parentErrors && parentErrors[this.member];
  }

  getIsReadonly(): boolean {
    const readonlyProperties = (this.parentObject as ModifiableEntity).readonlyProperties;
    return readonlyProperties != null && readonlyProperties.contains(this.member as string);
  }

  setError(value: string | undefined) {
    const parent = this.parentObject as ModifiableEntity;

    if (!value) {
      if (parent.error)
        delete parent.error[this.member];
    } else {
      if (!parent.Type)
        return;

      if (!parent.error)
        parent.error = {};

      parent.error[this.member] = value;
    }
  }
}


export class ReadonlyBinding<T> implements IBinding<T> {
  constructor(
    public value: T,
    public suffix: string) {
  }

  getValue() {
    return this.value;
  }
  setValue(val: T) {
    throw new Error("Readonly Binding");
  }

  getIsReadonly() {
    return true;
  }

  getError(): string | undefined {
    return undefined;
  }

  setError(value: string | undefined): void {
  }
}

export class MListElementBinding<T> implements IBinding<T>{

  suffix: string;
  constructor(
    public mListBinding: IBinding<MList<T>>,
    public index: number) {
    this.suffix = "[" + this.index.toString() + "].element";
  }

  getValue() {
    var mlist = this.mListBinding.getValue();

    if (mlist.length <= this.index) //Some animations?
      return undefined as any as T;

    return mlist[this.index].element;
  }

  getMListElement(): MListElement<T> {
    var mlist = this.mListBinding.getValue();
    return mlist[this.index];
  }

  setValue(val: T) {
    var mlist = this.mListBinding.getValue()
    const mle = mlist[this.index];
    mle.rowId = null;
    mle.element = val;
    this.mListBinding.setValue(mlist);
  }

  getIsReadonly() {
    return false; //Not sure
  }

  getError(): string | undefined {
    return undefined;
  }

  setError(value: string | undefined): void {
  }
}

export function createBinding(parentValue: any, lambdaMembers: LambdaMember[]): IBinding<any> {

  if (lambdaMembers.length == 0)
    return new ReadonlyBinding<any>(parentValue, "");
  var suffix = "";
  let val = parentValue;

  var lastIsIndex = lambdaMembers[lambdaMembers.length - 1].type == "Indexer";

  for (let i = 0; i < lambdaMembers.length - (lastIsIndex ? 2 : 1); i++) {
    const member = lambdaMembers[i];
    switch (member.type) {

      case "Member":
        val = val[member.name];
        suffix += "." + member.name;
        break;
      case "Mixin":
        val = val.mixins[member.name];
        suffix += ".mixins[" + member.name + "]";
        break;
      case "Indexer":
        val = val[parseInt(member.name)];
        suffix += "[" + member.name + "].element";
        break;
      default: throw new Error("Unexpected " + member.type);

    }
  }

  const lastMember = lambdaMembers[lambdaMembers.length - 1];
  switch (lastMember.type) {

    case "Member": return new Binding(val, lastMember.name, suffix + "." + lastMember.name);
    case "Mixin": return new ReadonlyBinding(val.mixins[lastMember.name], suffix + ".mixins[" + lastMember.name + "]");
    case "Indexer":
      const preLastMember = lambdaMembers[lambdaMembers.length - 2];
      var binding = new Binding<MList<any>>(val, preLastMember.name, suffix + "." + preLastMember.name)
      return new MListElementBinding(binding, parseInt(lastMember.name));
    default: throw new Error("Unexpected " + lastMember.type);
  }
}

const functionRegex = /^function\s*\(\s*(?<param>[$a-zA-Z_][0-9a-zA-Z_$]*)\s*\)\s*{\s*(\"use strict\"\;)?\s*(var [^;]*;)?\s*return\s*(?<body>[^;]*)\s*;?\s*}$/;
const lambdaRegex = /^\s*\(?\s*(?<param>[$a-zA-Z_][0-9a-zA-Z_$]*)\s*\)?\s*=>\s*(({\s*(\"use strict\"\;)?\s*(var [^;]*;)?\s*return\s*(?<body>[^;]*)\s*;?\s*})|(?<body2>[^;]*))\s*$/;
const memberRegex = /^(.*)\.([$a-zA-Z_][0-9a-zA-Z_$]*)$/;
const memberIndexerRegex = /^(.*)\["([$a-zA-Z_][0-9a-zA-Z_$]*)"\]$/;
const mixinMemberRegex = /^(.*)\.mixins\["([$a-zA-Z_][0-9a-zA-Z_$]*)"\]$/; //Necessary for some crazy minimizers
const indexRegex = /^(.*)\[(\d+)\]$/;
const fixNullPropagator = /^\(([_\w]+)\s*=\s(.*?)\s*\)\s*===\s*null\s*\|\|\s*\1\s*===\s*void 0\s*\?\s*void 0\s*:\s*\1$/;
const fixNullPropagatorProd = /^\s*null\s*===\(([_\w]+)\s*=\s*(.*?)\s*\)\s*\|\|\s*void 0\s*===\s*\1\s*\?\s*void 0\s*:\s*\1$/;

export function getLambdaMembers(lambda: Function): LambdaMember[] {

  var lambdaStr = (lambda as any).toString();

  const lambdaMatch = functionRegex.exec(lambdaStr) || lambdaRegex.exec(lambdaStr);

  if (lambdaMatch == undefined)
    throw Error("invalid function");

  const parameter = lambdaMatch.groups!.param;
  let body = lambdaMatch.groups!.body ?? lambdaMatch.groups!.body2;
  let result: LambdaMember[] = [];

  while (body != parameter) {
    let m: RegExpExecArray | null;
    if (m = mixinMemberRegex.exec(body)) {
      result.push({ name: m[2], type: "Mixin" });
      body = m[1];
    }
    else if (m = memberRegex.exec(body) ?? memberIndexerRegex.exec(body)) {
      result.push({ name: m[2], type: "Member" });
      body = m[1];
    }
    else if (m = indexRegex.exec(body)) {
      result.push({ name: m[2], type: "Indexer" });
      body = m[1];
    }
    else if (m = fixNullPropagator.exec(body) ?? fixNullPropagatorProd.exec(body)) {
      body = m[2];
    }
    else {
      throw new Error(`Impossible to extract the properties from: ${body}` +
        (body.contains("Mixin") ? "\r\n Consider using subCtx(MyMixin) directly." : null));
    }
  }

  result = result.reverse();

  result = result.filter((m, i) => !(m.type == "Member" && m.name == "element" && i > 0 && result[i - 1].type == "Indexer"));

  return result;
}

//slighliy different to is, and prevents webpack cycle
function sameEntity(a: any, b: any) {

  if (a === b)
    return true;

  if (a == undefined || b == undefined)
    return false;

  if ((a as Entity).Type && (b as Entity).Type) {
    return (a as Entity).Type == (b as Entity).Type &&
      (a as Entity).id != null && (b as Entity).id != null &&
      (a as Entity).id == (b as Entity).id;
  }

  if ((a as Lite<Entity>).EntityType && (b as Lite<Entity>).EntityType) {
    return (a as Lite<Entity>).EntityType == (b as Lite<Entity>).EntityType &&
      (a as Lite<Entity>).id != null && (b as Lite<Entity>).id != null &&
      (a as Lite<Entity>).id == (b as Lite<Entity>).id;
  }

  return false;
}

export function getFieldMembers(field: string): LambdaMember[] {
  if (field.contains(".")) {
    var mixinType = field.before(".").after("[").before("]");
    var fieldName = field.after(".");

    return [
      { type: "Mixin", name: mixinType },
      { type: "Member", name: fieldName.firstLower() }
    ];

  } else {
    return [
      { type: "Member", name: field.firstLower() }
    ];
  }
}




export interface LambdaMember {
  name: string;
  type: MemberType
}

export type MemberType = "Member" | "Mixin" | "Indexer";

export function New(type: PseudoType, props?: any, propertyRoute?: PropertyRoute): ModifiableEntity {

  const ti = tryGetTypeInfo(type);

  const result = { Type: getTypeName(type), isNew: true, modified: true } as any as ModifiableEntity;

  if (ti) {

    var e = result as Entity;
    var pr = PropertyRoute.root(ti);
    initializeMixins(e, pr);
    initializeCollections(e, pr);
  }
  else {
    if (propertyRoute) {
      initializeCollections(result, propertyRoute);
      initializeMixins(result, propertyRoute);
    } else {
      //Collections or mixins not initialized, but since Embedded typically don't have then, it's not worth the hassle 
    }
  }

  if (props) {
    Dic.assign(result, props);
    result.Type = getTypeName(type);
    result.modified = true;
    result.isNew = true;
  }

  return result;
}

function initializeMixins(mod: ModifiableEntity, pr: PropertyRoute) {
  var subMembers = pr.subMembers();
  Dic.getKeys(subMembers)
    .filter(a => a.startsWith("["))
    .groupBy(a => a.after("[").before("]"))
    .forEach(gr => {

      var mixin = ({ Type: gr.key, isNew: true, modified: true, }) as MixinEntity;

      initializeCollections(mixin, pr.addMember("Mixin", gr.key, true)!);

      if (!mod.mixins)
        mod.mixins = {};

      mod.mixins[gr.key] = mixin;
    });

}

function initializeCollections(mod: ModifiableEntity, pr: PropertyRoute) {
  Dic.map(pr.subMembers(), (key, memberInfo) => ({ key, memberInfo }))
    .filter(t => t.memberInfo.type.isCollection && !t.key.startsWith("["))
    .forEach(t => (mod as any)[t.key.firstLower()] = []);
}


export function clone<T>(original: ModifiableEntity, propertyRoute?: PropertyRoute) {
  const ti = tryGetTypeInfo(original.Type);

  const result = { Type: original.Type, isNew: true, modified: true } as any as ModifiableEntity;

  if (ti) {

    var e = result as Entity;

    var pr = PropertyRoute.root(ti);

    const mixins = Dic.getKeys(ti.members)
      .filter(a => a.startsWith("["))
      .groupBy(a => a.after("[").before("]"))
      .forEach(gr => {

        var m = ({ Type: gr.key, isNew: true, modified: true, }) as MixinEntity;

        copyProperties(m, (original as Entity).mixins![gr.key], pr.addMember("Mixin", gr.key, true));

        if (!e.mixins)
          e.mixins = {};

        e.mixins[gr.key] = m;
      });

    copyProperties(e, original, pr);
  }
  else {
    if (!propertyRoute) {
      throw new Error("propertyRoute is mandatory for non-Entities");
    }

    copyProperties(result, original, propertyRoute);
  }

  return result;
}

function copyProperties(result: any, original: any, pr: PropertyRoute) {
  Dic.map(pr.subMembers(), (key, memberInfo) => ({ key, memberInfo }))
    .filter(p => p.key != "Id")
    .forEach(t => {
      var memberName = t.key.firstLower();
      var orinalProp = (original as any)[memberName];
      var clonedProp = cloneIfNeeded(orinalProp, pr.addMember("Member", t.key, true));
      (result as any)[memberName] = clonedProp;
    });
}

function cloneCollection<T>(mlist: MList<T>, propertyRoute: PropertyRoute): MList<T> {

  var elemPr = PropertyRoute.mlistItem(propertyRoute);

  if (propertyRoute.member!.isVirtualMList) {

    return mlist.map(mle => ({ rowId: null, element: clone(mle.element as any as Entity, elemPr) as any as T }));
  }

  return mlist.map(mle => ({ rowId: null, element: cloneIfNeeded(mle.element, elemPr) as T }));
}

function cloneIfNeeded(original: any, pr: PropertyRoute) {

  var tr = pr.typeReference();
  if (tr.isCollection)
    return cloneCollection(original, pr);

  if (original === null || original === undefined)
    return original;

  if (tr.isEmbedded)
    return clone(original, pr);

  if (tr.name == IsByAll || tryGetTypeInfos(tr.name).length > 0)
    return JSON.parse(JSON.stringify(original));

  return original; //string, number, boolean, etc...
}


export interface IType {
  typeName: string;
}

export function isType(obj: any): obj is IType {
  return (obj as IType).typeName != undefined;
}

export function newLite<T extends Entity>(type: Type<T>, id: number | string, model?: unknown): Lite<T>;
export function newLite(typeName: PseudoType, id: number | string, model?: unknown): Lite<Entity>;
export function newLite(type: PseudoType, id: number | string, model?: unknown): Lite<Entity> {
  return {
    EntityType: getTypeName(type),
    id: id,
    model: model
  };
}

export type Anonymous<T extends ModifiableEntity> = T & {
  /** Represents the 'Entity' column in the query selector */
  entity: T
}

export class Type<T extends ModifiableEntity> implements IType {

  New(props?: Partial<T>, propertyRoute?: PropertyRoute): T {

    if (props && props.Type && (propertyRoute || tryGetTypeInfo(props.Type))) {
      if (props.Type != this.typeName)
        throw new Error("Cloning with another type");
      return clone(props as ModifiableEntity, propertyRoute) as T;
    }

    return New(this.typeName, props, propertyRoute) as T;
  }

  constructor(
    public typeName: string) { }

  tryTypeInfo(): TypeInfo | undefined {
    return tryGetTypeInfo(this.typeName);
  }

  typeInfo(): TypeInfo {

    const result = this.tryTypeInfo();

    if (!result)
      throw new Error(`Type ${this.typeName} has no TypeInfo. \nNote: If is an EmbeddedEntity, start from some main Entity Type containing it to get metadata for the embedded properties (example: MyEntity.propertyRoute(m => m.myEmbedded.someProperty)`);

    return result;
  }

  memberInfo(lambdaToProperty: (v: T) => any): MemberInfo {
    var pr = this.propertyRouteAssert(lambdaToProperty);

    if (!pr.member)
      throw new Error(`${pr.propertyPath()} has no member`);

    return pr.member;
  }

  tryMemberInfo(lambdaToProperty: (v: T) => any): MemberInfo | undefined {
    var pr = this.tryPropertyRoute(lambdaToProperty);

    return pr?.member;
  }

  hasMixin(mixinType: Type<MixinEntity>): boolean {
    return Dic.getKeys(this.typeInfo().members).some(k => k.startsWith("[" + mixinType.typeName + "]"));
  }

  mixinMemberInfo<M extends MixinEntity>(mixinType: Type<M>, lambdaToProperty: (v: M) => any): MemberInfo {
    var pr = this.mixinPropertyRouteAssert(mixinType, lambdaToProperty);

    if (!pr.member)
      throw new Error(`${pr.propertyPath()} has no member`);

    return pr.member;
  }

  tryMixinMemberInfo<M extends MixinEntity>(mixinType: Type<M>, lambdaToProperty: (v: M) => any): MemberInfo | undefined {
    var pr = this.tryMixinPropertyRoute(mixinType, lambdaToProperty);

    return pr?.member;
  }

  propertyRouteAssert(lambdaToProperty: (v: T) => any): PropertyRoute {
    return PropertyRoute.root(this.typeInfo()).addLambda(lambdaToProperty);
  }

  tryPropertyRoute(lambdaToProperty: (v: T) => any): PropertyRoute | undefined {

    var ti = this.tryTypeInfo();
    if (ti == null)
      return undefined;
    return PropertyRoute.root(ti).tryAddLambda(lambdaToProperty);
  }

  mixinPropertyRouteAssert<M extends MixinEntity>(mixinType: Type<M>, lambdaToProperty: (v: M) => any): PropertyRoute {
    return PropertyRoute.root(this.typeInfo()).addMember("Mixin", mixinType.typeName, true).addLambda(lambdaToProperty);
  }

  tryMixinPropertyRoute<M extends MixinEntity>(mixinType: Type<M>, lambdaToProperty: (v: M) => any): PropertyRoute | undefined {
    var ti = this.tryTypeInfo();
    if (ti == null)
      return undefined;

    return PropertyRoute.root(ti).addMember("Mixin", mixinType.typeName, false)?.tryAddLambda(lambdaToProperty);
  }

  niceName(): string {
    const ti = this.typeInfo();

    if (!ti.niceName)
      throw new Error(`no niceName found for ${ti.name}`);

    return ti.niceName;
  }

  nicePluralName(): string {
    const ti = this.typeInfo();

    if (!ti.nicePluralName)
      throw new Error(`no nicePluralName found for ${ti.name}`);

    return ti.nicePluralName;
  }

  niceCount(count: number): string {
    return count + " " + (count == 1 ? this.niceName() : this.nicePluralName());
  }

  nicePropertyName(lambdaToProperty: (v: T) => any): string {
    const member = this.memberInfo(lambdaToProperty);

    if (!member.niceName)
      throw new Error(`no nicePropertyName found for ${member.name}`);

    return member.niceName;
  }

  isInstance(obj: any): obj is T {
    return obj && (obj as ModifiableEntity).Type == this.typeName;
  }

  cast(obj: any): T {
    if (this.isInstance(obj))
      return obj;

    throw new Error("Unable to cast object to " + this.typeName);
  }

  isLite(obj: any): obj is Lite<T & Entity> {
    return obj && (obj as Lite<Entity>).EntityType == this.typeName;
  }

  /* Constructs a QueryToken able to generate string like "Name" from a strongly typed lambda like a => a.name
   * Note: The QueryToken language is quite different to javascript lambdas (Any, Lites, Nullable, etc)*/
  token(): QueryTokenString<Anonymous<T>>;
  /** Shortcut for token().append(lambdaToColumn)
   * @param lambdaToColumn lambda expression pointing to a property in the anonymous class of the query. For simple columns comming from properties from the entity.
   */
  token<S>(lambdaToColumn: (v: Anonymous<T>) => S): QueryTokenString<S>;
  /** Shortcut for token().expression<S>(columnName)
  * @param columnName property name of some property in the anonymous class of the query. For complex calculated columns that are not a property from the entitiy.
  */
  token<S>(columnName: string): QueryTokenString<S>;
  token(lambdaToColumn?: ((a: any) => any) | string): QueryTokenString<any> {
    if (lambdaToColumn == null)
      return new QueryTokenString("");
    else if (typeof lambdaToColumn == "string")
      return new QueryTokenString(lambdaToColumn);
    else
      return new QueryTokenString(tokenSequence(lambdaToColumn, true));
  }

  toString() {
    return this.typeName;
  }
}

/*  Some examples being in ExceptionEntity:
 *  "User" -> ExceptionEntity.token().append(a => a.user) 
 *            ExceptionEntity.token(a => a.user) 
 *  "Entity.User" -> ExceptionEntity.token().append(a=>a.entity).append(a=>a.user)
 *                   ExceptionEntity.token(a => a.entity.user)
 * 
 */
export class QueryTokenString<T> {

  token: string;
  constructor(token: string) {
    this.token = token;
  }

  toString() {
    return this.token;
  }

  static entity<T extends Entity = Entity>() {
    return new QueryTokenString<T>("Entity");
  }

  static count() {
    return new QueryTokenString("Count");
  }

  systemValidFrom() {
    return new QueryTokenString(this.token + ".SystemValidFrom");
  }

  systemValidTo() {
    return new QueryTokenString(this.token + ".SystemValidTo");
  }

  getToString() {
    return new QueryTokenString(this.token + ".ToString");
  }

  cast<R extends Entity>(t: Type<R>): QueryTokenString<R> {
    return new QueryTokenString<R>(this.token + ".(" + t.typeName + ")");
  }

  /**
   * Allows adding some extra property names to a QueryTokenString
   * @param lambdaToProperty for a typed lambda like a => a.name will append "Name" to the QueryTokenString
   */
  append<S>(lambdaToProperty: (v: T) => S): QueryTokenString<S> {
    return new QueryTokenString<S>(this.token + (this.token ? "." : "") + tokenSequence(lambdaToProperty, !this.token));
  }

  mixin<M extends MixinEntity>(t: Type<M>): QueryTokenString<M> {
    return new QueryTokenString<M>(this.token);
  }

  /**
  * Allows to add an extra token to a QueryTokenString given the name and the type. Typically used for registered expressions. Not strongly-typed :(
  * @param expressionName name of the token to add (typically a registered expression)
  */
  expression<S>(expressionName: string): QueryTokenString<S> {
    return new QueryTokenString<S>(this.token + (this.token ? "." : "") + expressionName);
  }

  any<S = ArrayElement<T>>(): QueryTokenString<S> {
    return new QueryTokenString<S>(this.token + ".Any");
  }

  all<S = ArrayElement<T>>(): QueryTokenString<S> {
    return new QueryTokenString<S>(this.token + ".All");
  }

  anyNo<S = ArrayElement<T>>(): QueryTokenString<S> {
    return new QueryTokenString<S>(this.token + ".AnyNo");
  }

  separatedByComma<S = ArrayElement<T>>(): QueryTokenString<S> {
    return new QueryTokenString<S>(this.token + ".SeparatedByComma");
  }

  separatedByCommaDistinct<S = ArrayElement<T>>(): QueryTokenString<S> {
    return new QueryTokenString<S>(this.token + ".SeparatedByCommaDistinct");
  }

  separatedByNewLine<S = ArrayElement<T>>(): QueryTokenString<S> {
    return new QueryTokenString<S>(this.token + ".SeparatedByNewLine");
  }

  separatedByNewLineDistinct<S = ArrayElement<T>>(): QueryTokenString<S> {
    return new QueryTokenString<S>(this.token + ".SeparatedByNewLineDistinct");
  }
  

  noOne<S = ArrayElement<T>>(): QueryTokenString<S> {
    return new QueryTokenString<S>(this.token + ".NoOne");
  }

  element<S = ArrayElement<T>>(index = 1): QueryTokenString<S> {
    return new QueryTokenString<S>(this.token + (this.token ? "." : "") + "Element" + (index == 1 ? "" : index));
  }

  count(option?: "Distinct" | "Null" | "NotNull"): QueryTokenString<number> {
    return new QueryTokenString<number>(this.token + (this.token ? "." : "") + "Count" + ((option == undefined) ? "" : option));
  }

  min(): QueryTokenString<T> {
    return new QueryTokenString<T>(this.token + ".Min");
  }

  max(): QueryTokenString<T> {
    return new QueryTokenString<T>(this.token + ".Max");
  }

  sum(): QueryTokenString<T> {
    return new QueryTokenString<T>(this.token + ".Sum");
  }

  average(): QueryTokenString<T> {
    return new QueryTokenString<T>(this.token + ".Average");
  }

  hasValue(): QueryTokenString<boolean> {
    return new QueryTokenString<boolean>(this.token + ".HasValue");
  }

  operation(os: OperationSymbol): string { //operation tokens are leaf
    return this.token + ".[Operations]." + os.key.replace(".", "#");
  }
}

type ArrayElement<ArrayType> = ArrayType extends (infer ElementType)[] ? RemoveMListElement<ElementType> : never;

type RemoveMListElement<Type> = Type extends MListElement<infer S> ? S : Type;

function tokenSequence(lambdaToProperty: Function, isFirst: boolean) {
  return getLambdaMembers(lambdaToProperty)
    .filter((a, i) => a.name != "entity" || i == 0 && isFirst) //For convinience navigating Lite<T>, 'entity' is removed. If you have a property named Entity, you will need to use expression<S>()
    .map(a => a.name == "toStr" ? "ToString" : a.name.firstUpper())
    .join(".");
}

export class EnumType<T extends string> {
  constructor(public type: string) { }

  typeInfo(): TypeInfo {
    return getTypeInfo(this.type);
  }

  values(): T[] {
    return Dic.getKeys(this.typeInfo().members) as T[];
  }

  isDefined(val: any): val is T {
    return typeof val == "string" && this.typeInfo().members[val] != null
  }

  assertDefined(val: any): T {
    if (this.isDefined(val))
      return val;

    throw new Error(`'${val}' is not a valid ${this.type}`)
  }

  value(val: T): T {
    return val;
  }

  niceTypeName(): string | undefined {
    return this.typeInfo().niceName;
  }

  niceToString(value: T): string {
    return this.typeInfo().members[value as string].niceName;
  }
}

export class MessageKey {

  constructor(
    public type: string,
    public name: string) { }

  propertyInfo(): MemberInfo {
    return getTypeInfo(this.type).members[this.name]
  }

  niceToString(...args: any[]): string {
    const msg = this.propertyInfo().niceName;

    return args.length ? msg.formatWith(...args) : msg;
  }
}

export class QueryKey {

  constructor(
    public type: string,
    public name: string) { }

  memberInfo(): MemberInfo {
    return getTypeInfo(this.type).members[this.name]
  }

  niceName(): string {
    return this.memberInfo().niceName;
  }
}

export interface ISymbol {
  Type: string;
  key: string;
  id?: any;
}

let missingSymbols: ISymbol[] = [];

function getMember(key: string): MemberInfo | undefined {

  if (!key.contains("."))
    return undefined;

  const type = _types[key.before(".").toLowerCase()];

  if (!type)
    return undefined;

  const member: MemberInfo | undefined = type.members[key.after(".")];

  return member;
}

export function symbolNiceName(symbol: Entity & ISymbol | Lite<Entity & ISymbol>): string {
  if ((symbol as Entity).Type != null) //Don't use isEntity to avoid cycle
  {
    var m = getMember((symbol as Entity & ISymbol).key);
    return m && m.niceName || (symbol as Entity).toStr!;
  }
  else {
    var model = (symbol as Lite<Entity>).model;
    var toStr = typeof model == "string" ? model : (model as ModelEntity).toStr;

    var m = getMember(toStr);
    return m && m.niceName || toStr;
  }
}

export function getSymbol<T extends Entity & ISymbol>(type: Type<T>, key: string) { //Unsafe Type!

  const mi = getMember(key);
  if (mi == null)
    throw new Error(`No Symbol with key '${key}' found`);

  var symbol = {
    Type: type.typeName,
    id: mi.id,
    key: key
  } as T;
  return symbol as T
}

export function registerSymbol(type: string, key: string): any /*ISymbol*/ {

  const mi = getMember(key);

  var symbol = {
    Type: type,
    id: mi && mi.id || null,
    key: key
  } as ISymbol;

  if (symbol.id == null)
    missingSymbols.push(symbol);

  return symbol as any;
}




export class PropertyRoute {

  propertyRouteType: PropertyRouteType;
  parent?: PropertyRoute; //!Root
  rootType?: TypeInfo; //Root
  member?: MemberInfo; //Member
  mixinName?: string; //Mixin

  static root(type: PseudoType) {
    const typeInfo = getTypeInfo(type);
    if (!typeInfo) {
      throw Error(`No TypeInfo for "${getTypeName(type)}" found. Consider calling ReflectionServer.RegisterLike on the server side.`);
    }
    return new PropertyRoute(undefined, "Root", typeInfo, undefined, undefined);
  }

  static member(parent: PropertyRoute, member: MemberInfo) {
    return new PropertyRoute(parent, "Field", undefined, member, undefined);
  }

  static mixin(parent: PropertyRoute, mixinName: string) {
    return new PropertyRoute(parent, "Mixin", undefined, undefined, mixinName);
  }

  static mlistItem(parent: PropertyRoute) {
    return new PropertyRoute(parent, "MListItem", undefined, undefined, undefined);
  }

  static liteEntity(parent: PropertyRoute) {
    return new PropertyRoute(parent, "LiteEntity", undefined, undefined, undefined);
  }



  addMembers(propertyString: string): PropertyRoute {
    let result: PropertyRoute = this;

    const parts = PropertyRoute.parseLambdaMembers(propertyString);

    parts.forEach(m => result = result.addMember(m.type, m.name, true));

    return result;
  }

  tryAddMembers(propertyString: string): PropertyRoute | undefined {
    let result: PropertyRoute | undefined = this;

    const parts = PropertyRoute.parseLambdaMembers(propertyString);

    parts.forEach(m => result = result?.addMember(m.type, m.name, false));

    return result;
  }

  static parseLambdaMembers(propertyString: string) {
    function splitMixin(text: string): LambdaMember[] {

      if (text.length == 0)
        return [];

      if (text.contains("["))
        return [
          ...splitMixin(text.before("[")),
          { type: "Mixin" as MemberType, name: text.between("[", "]") },
          ...splitMixin(text.after("]")),
        ];

      return [{ type: "Member", name: text }];
    }

    function splitDot(text: string): LambdaMember[] {
      if (text.contains("."))
        return [
          ...splitMixin(text.before(".")),
          ...splitDot(text.after("."))
        ];

      return splitMixin(text);
    }

    function splitIndexer(text: string): LambdaMember[] {
      if (text.contains("/"))
        return [
          ...splitDot(text.before("/")),
          { type: "Indexer", name: "0" },
          ...splitIndexer(text.after("/"))
        ];

      return splitDot(text);
    }
    return splitIndexer(propertyString);
  }


  static parseFull(fullPropertyRoute: string): PropertyRoute {
    const type = fullPropertyRoute.after("(").before(")");
    let propertyString = fullPropertyRoute.after(")");
    if (propertyString.startsWith("."))
      propertyString = propertyString.substr(1);
    return PropertyRoute.root(type).addMembers(propertyString);
  }

  static parse(rootType: PseudoType, propertyString: string): PropertyRoute {
    return PropertyRoute.root(rootType).addMembers(propertyString);
  }

  static tryParseFull(fullPropertyRoute: string): PropertyRoute | undefined {
    const type = fullPropertyRoute.after("(").before(")");
    let propertyString = fullPropertyRoute.after(")");
    if (propertyString.startsWith("."))
      propertyString = propertyString.substr(1);

    var ti = tryGetTypeInfo(type);
    if (ti == null)
      return undefined;

    return PropertyRoute.tryParse(type, propertyString);
  }

  static tryParse(rootType: PseudoType, propertyString: string): PropertyRoute | undefined {

    const ti = tryGetTypeInfo(rootType);
    if (ti == null)
      return undefined;

    return PropertyRoute.root(ti).tryAddMembers(propertyString);
  }

 

  constructor(
    parent: PropertyRoute | undefined,
    propertyRouteType: PropertyRouteType,
    rootType: TypeInfo | undefined,
    member: MemberInfo | undefined,
    mixinName: string | undefined) {

    this.propertyRouteType = propertyRouteType;
    this.parent = parent;
    this.rootType = rootType;
    this.member = member;
    this.mixinName = mixinName;
  }

  allParents(includeMixins = false): PropertyRoute[] {

    if (!includeMixins && this.propertyRouteType == "Mixin")
      return this.parent!.allParents(includeMixins);

    return [...this.parent == null ? [] : this.parent.allParents(includeMixins), this];
  }

  addMixin<T extends MixinEntity>(mixin: Type<T>, property: ((val: T) => any) | string) {
    return this.addMember("Mixin", mixin.typeName, true).addLambda(property);
  }

  addLambda(property: ((val: any) => any) | string): PropertyRoute {
    const lambdaMembers = typeof property == "function" ?
      getLambdaMembers(property) :
      getFieldMembers(property);

    let result: PropertyRoute = lambdaMembers.reduce<PropertyRoute>((pr, m) => pr.addLambdaMember(m), this)

    return result;
  }

  tryAddLambda(property: ((val: any) => any) | string): PropertyRoute | undefined {
    const lambdaMembers = typeof property == "function" ?
      getLambdaMembers(property) :
      getFieldMembers(property);

    let result: PropertyRoute | undefined = lambdaMembers.reduce<PropertyRoute | undefined>((pr, m) => pr && pr.tryAddLambdaMember(m), this)

    return result;
  }

  typeReference(): TypeReference {
    switch (this.propertyRouteType) {
      case "Root": return { name: this.rootType!.name };
      case "Field": return this.member!.type;
      case "Mixin": throw new Error("mixins can not be used alone");
      case "MListItem": return { ...this.parent!.typeReference(), isCollection: false };
      case "LiteEntity": return { ...this.parent!.typeReference(), isLite: false };
      default: throw new Error("Unexpected propertyRouteType");
    }
  }

  typeReferenceInfo(): TypeInfo {
    return getTypeInfo(this.typeReference().name);
  }

  findRootType(): TypeInfo {
    switch (this.propertyRouteType) {
      case "Root": return this.rootType!;
      case "Field": return this.parent!.findRootType();
      case "Mixin": return this.parent!.findRootType();
      case "MListItem": return this.parent!.findRootType();
      case "LiteEntity": return this.parent!.findRootType();
      default: throw new Error("Unexpected propertyRouteType");
    }
  }

  propertyPath(): string {
    switch (this.propertyRouteType) {
      case "Root": throw new Error("Root has no PropertyString");
      case "Field": return this.member!.name;
      case "Mixin": return (this.parent!.propertyRouteType == "Root" ? "" : this.parent!.propertyPath()) + "[" + this.mixinName + "]";
      case "MListItem": return this.parent!.propertyPath() + "/";
      case "LiteEntity": return this.parent!.propertyPath() + ".entity";
      default: throw new Error("Unexpected propertyRouteType");
    }
  }

  tryAddMember(memberType: MemberType, memberName: string): PropertyRoute | undefined {
    try {
      return this.addMember(memberType, memberName, false);
    } catch (e) {
      return undefined;
    }
  }

  addLambdaMember(lm: LambdaMember): PropertyRoute {
    return this.addMember(lm.type, lm.type == "Member" ? toCSharp(lm.name) : lm.name, true);
  }

  tryAddLambdaMember(lm: LambdaMember): PropertyRoute | undefined {
    return this.addMember(lm.type, lm.type == "Member" ? toCSharp(lm.name) : lm.name, false);
  }

  addMember(memberType: MemberType, memberName: string, throwIfNotFound: true): PropertyRoute;
  addMember(memberType: MemberType, memberName: string, throwIfNotFound: false): PropertyRoute | undefined;
  addMember(memberType: MemberType, memberName: string, throwIfNotFound: boolean): PropertyRoute | undefined {

    var getErrorContext = () => ` (adding ${memberType} ${memberName} to ${this.toString()})`;

    if (memberType == "Member" || memberType == "Mixin") {

      if (this.propertyRouteType == "Field" ||
        this.propertyRouteType == "MListItem" ||
        this.propertyRouteType == "LiteEntity") {
        const ref = this.typeReference();

        if (ref.isLite) {
          if (memberType == "Member" && memberName == "Entity")
            return PropertyRoute.liteEntity(this);
          
          throw new Error("Entity expected");
        }

        const ti = tryGetTypeInfos(ref).single("Ambiguity due to multiple Implementations" + getErrorContext()); //[undefined]
        if (ti) {

          if (memberType == "Mixin")
            return PropertyRoute.mixin(this, memberName);
          else {
            const m = ti.members[memberName];
            if (!m) {
              if (throwIfNotFound)
                throw new Error(`member '${memberName}' not found` + getErrorContext());
              return undefined;
            }
            return PropertyRoute.member(PropertyRoute.root(ti), m);
          }
        } else if (this.propertyRouteType == "LiteEntity") {
          throw Error("Unexpected lite case" + getErrorContext());
        }
      }

      if (memberType == "Mixin")
        return PropertyRoute.mixin(this, memberName);
      else {
        const fullMemberName =
          this.propertyRouteType == "Root" ? memberName :
            this.propertyRouteType == "MListItem" ? this.propertyPath() + memberName :
              this.propertyPath() + "." + memberName;

        const m = this.findRootType().members[fullMemberName];
        if (!m) {
          if (throwIfNotFound)
            throw new Error(`member '${fullMemberName}' not found` + getErrorContext());

          return undefined;
        }
        return PropertyRoute.member(this, m);
      }
    }

    if (memberType == "Indexer") {
      if (this.propertyRouteType != "Field")
        throw new Error("invalid indexer at this stage" + getErrorContext());

      const tr = this.typeReference();
      if (!tr.isCollection)
        throw new Error("${this.propertyPath()} is not a collection" + getErrorContext());

      return PropertyRoute.mlistItem(this);
    }

    throw new Error("not implemented" + getErrorContext());
  }

  static generateAll(type: PseudoType): PropertyRoute[] {
    var ti = getTypeInfo(type);
    var mixins: string[] = [];
    return Dic.getValues(ti.members).flatMap(mi => {
      const pr = PropertyRoute.parse(ti, mi.name);
      if (pr.typeReference().isCollection)
        return [pr, PropertyRoute.mlistItem(pr)];
      return [pr];
    }).flatMap(pr => {
      if (pr.parent && pr.parent.propertyRouteType == "Mixin" && !mixins.contains(pr.parent.propertyPath())) {
        mixins.push(pr.parent.propertyPath());
        return [pr.parent, pr];
      } else
        return [pr];
    });
  }

  subMembers(): { [subMemberName: string]: MemberInfo } {

    function simpleMembersAfter(type: TypeInfo, path: string) {
      return Dic.getValues(type.members)
        .filter(m => {
          if (m.name == path || !m.name.startsWith(path))
            return false;

          var suffix = m.name.substring(path.length);
          if (suffix.contains("/"))
            return false;

          if (suffix.startsWith("[") ? suffix.after("].").contains("."): suffix.contains("."))
            return false;

          return true;
        })
        .toObject(m => m.name.substring(path.length))
    }

    function mixinMembers(type: TypeInfo) {
      var mixins = Dic.getValues(type.members).filter(a => a.name.startsWith("[")).groupBy(a => a.name.after("[").before("]")).map(a => a.key);

      return mixins.flatMap(mn => Dic.getValues(simpleMembersAfter(type, `[${mn}].`))).toObject(a => a.name);
    }


    switch (this.propertyRouteType) {
      case "Root": return {
        ...simpleMembersAfter(this.findRootType(), ""),
        ...mixinMembers(this.findRootType())
      };
      case "Mixin": return simpleMembersAfter(this.findRootType(), this.propertyPath() + ".");
      case "LiteEntity": return simpleMembersAfter(this.typeReferenceInfo(), "");
      case "Field":
      case "MListItem":
        {
          var tr = this.typeReference();
          if (tr.name == IsByAll)
            return {};

          const ti = tryGetTypeInfos(this.typeReference()).single("Ambiguity due to multiple Implementations"); //[undefined]
          if (ti && (isTypeEntity(ti) || isTypeModel(ti)))
            return simpleMembersAfter(ti, "");
          else
            return simpleMembersAfter(this.findRootType(), this.propertyPath() + (this.propertyRouteType == "Field" ? "." : ""));
        }
      default: throw new Error("Unexpected propertyRouteType");

    }
  }

  toString() {
    var rootTypeName = this.findRootType().name;

    if (this.propertyRouteType == "Root")
      return `(${rootTypeName})`;

    var path = this.propertyPath();
    if (path.startsWith("["))
      return `(${rootTypeName})${this.propertyPath()}`;

    return `(${rootTypeName}).${this.propertyPath()}`;
  }
}

function toCSharp(name: string) {
  var result = name.firstUpper();

  if (result == name)
    throw new Error(`Name '${name}' should start by lowercase`);

  return result;
}

export type PropertyRouteType = "Root" | "Field" | "Mixin" | "LiteEntity" | "MListItem";

export type GraphExplorerMode = "collect" | "set" | "clean";


export class GraphExplorer {

  static hasChanges(m: ModifiableEntity) {
    this.propagateAll(m);
    return m.modified;
  }

  static propagateAll(...args: any[]): GraphExplorer {
    const ge = new GraphExplorer("clean", {});
    args.forEach(o => ge.isModified(o, ""));
    return ge;
  }

  static setModelState(e: ModifiableEntity, modelState: ModelState | undefined, initialPrefix: string) {
    const ge = new GraphExplorer("set", modelState == undefined ? {} : { ...modelState });
    ge.isModifiableObject(e, initialPrefix);
    if (Dic.getValues(ge.modelState).length) //Assign remaining
    {
      if (e.error == undefined)
        e.error = {};

      for (const key in ge.modelState) {
        e.error[key] = ge.modelState[key].join("\n");
        delete ge.modelState[key];
      }
    }
  }

  static collectModelState(e: ModifiableEntity, initialPrefix: string): ModelState {
    const ge = new GraphExplorer("collect", {});
    ge.isModifiableObject(e, initialPrefix);
    return ge.modelState;
  }

  //cycle detection
  private modified: any[] = [];
  private notModified: any[] = [];

  constructor(mode: GraphExplorerMode, modelState: ModelState) {
    this.modelState = modelState;
    this.mode = mode;
  }

  private mode: GraphExplorerMode;

  private modelState: ModelState;


  isModified(obj: any, modelStatePrefix: string): boolean {

    if (obj == undefined)
      return false;

    const t = typeof obj;
    if (t != "object")
      return false;

    if (this.modified.contains(obj)) {
      if (window.exploreGraphDebugMode)
        debugger;
      return true;
    }

    if (this.notModified.contains(obj))
      return false;

    const result = this.isModifiableObject(obj, modelStatePrefix);

    if (result) {
      if (window.exploreGraphDebugMode)
        debugger;
      this.modified.push(obj);
      return true;
    } else {
      this.notModified.push(obj);
      return false;
    }
  }

  private static specialProperties = ["Type", "id", "isNew", "ticks", "toStr", "modified"];

  //The redundant return true / return false are there for debugging
  private isModifiableObject(obj: any, modelStatePrefix: string) {

    if (obj instanceof Date)
      return false;

    if (obj instanceof Array) {
      let result = false;
      for (let i = 0; i < obj.length; i++) {
        if (this.isModified(obj[i], modelStatePrefix + "[" + i + "]")) {
          if (window.exploreGraphDebugMode)
            debugger;
          result = true;
        }
      }
      return result;
    }

    const mle = obj as MListElement<any>;
    if (mle.hasOwnProperty && mle.hasOwnProperty("rowId")) {
      if (this.isModified(mle.element, modelStatePrefix + ".element")) {
        if (window.exploreGraphDebugMode)
          debugger;
        return true;
      }


      if (mle.rowId == undefined) {
        if (window.exploreGraphDebugMode)
          debugger;
        return true;
      }

      return false;
    };

    const lite = obj as Lite<Entity>
    if (lite.EntityType) {
      if (lite.entity != undefined && this.isModified(lite.entity, modelStatePrefix + ".entity")) {
        if (window.exploreGraphDebugMode)
          debugger;
        return true;
      }

      return false;
    }

    const mod = obj as ModifiableEntity;
    if (mod.Type == undefined) { //Other object
      let result = false;
      for (const p in obj) {
        if (obj.hasOwnProperty == null || obj.hasOwnProperty(p)) {
          const propertyPrefix = modelStatePrefix + "." + p;
          if (this.isModified(obj[p], propertyPrefix)) {
            if (window.exploreGraphDebugMode)
              debugger;
            result = true;
          }
        }
      }

      return result;
    }

    if (this.mode == "collect") {
      if (mod.error != undefined) {
        for (const p in mod.error) {
          const propertyPrefix = modelStatePrefix + "." + p;

          if (mod.error[p])
            this.modelState[modelStatePrefix + "." + p] = [mod.error[p]];
        }
      }
    }
    else if (this.mode == "set") {

      mod.error = undefined;

      const prefix = modelStatePrefix  + ".";
      for (const key in this.modelState) {
        const propName = key.tryAfter(prefix)
        if (propName && !propName.contains(".")) {
          if (mod.error == undefined)
            mod.error = {};

          mod.error[propName] = this.modelState[key].join("\n");

          delete this.modelState[key];
        }
      }

      if (mod.error == undefined)
        delete mod.error;
    }
    else if (this.mode == "clean") {
      if (mod.error)
        delete mod.error
    }


    for (const p in obj) {
      if (obj.hasOwnProperty(p) && !GraphExplorer.specialProperties.contains(p)) {

        if (p == "mixins") {
          const propertyPrefix = modelStatePrefix + "." + p;
          if (this.isModifiedMixinDictionary(obj[p], propertyPrefix)) {
            if (window.exploreGraphDebugMode)
              debugger;
            mod.modified = true;
          }
        } else {
          const propertyPrefix = modelStatePrefix + "." + p;
          if (this.isModified(obj[p], propertyPrefix)) {
            if (window.exploreGraphDebugMode)
              debugger;
            mod.modified = true;
          }
        }
      }
    }

    if ((mod as Entity).isNew) {
      if (window.exploreGraphDebugMode)
        debugger;
      mod.modified = true; //Just in case

      if (GraphExplorer.TypesLazilyCreated.push((mod as Entity).Type))
        return false;
    }

    return mod.modified;
  }

  isModifiedMixinDictionary(mixins: { [name: string]: MixinEntity }, prefix: string) {
    if (mixins == undefined)
      return false;

    var modified = false;
    for (const p in mixins) {
      const mixinPrefix = prefix + "[" + p + "]";
      if (this.isModified(mixins[p], mixinPrefix)) { 
        if (window.exploreGraphDebugMode)
          debugger;
        modified = true;
      }
    }
    return modified;
  }

  static TypesLazilyCreated: string[] = [];
}

