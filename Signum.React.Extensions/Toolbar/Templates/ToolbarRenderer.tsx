import * as React from 'react'
import * as History from 'history'
import * as AppContext from '@framework/AppContext'
import * as ToolbarClient from '../ToolbarClient'
import { ToolbarConfig } from "../ToolbarClient";
import '@framework/Frames/MenuIcons.css'
import './Toolbar.css'
import { Nav } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useAPI, useUpdatedRef, useHistoryListen } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'
import { parseIcon } from '../../Basics/Templates/IconTypeahead'
import { SidebarMode  } from '../SidebarContainer'
import { isActive } from '@framework/FindOptions';

function isCompatibleWithUrl(r: ToolbarClient.ToolbarResponse<any>, location: History.Location, query: any): boolean {
  if (r.url)
    return (location.pathname + location.search).startsWith(AppContext.toAbsoluteUrl(r.url));

  if (!r.content)
    return false;

  var config = ToolbarClient.getConfig(r);
  if (!config)
    return false;

  return config.isCompatibleWithUrl(r, location, query);
}

function inferActive(r: ToolbarClient.ToolbarResponse<any>, location: History.Location, query: any): ToolbarClient.ToolbarResponse<any> | null {
  if (r.elements)
    return r.elements.map(e => inferActive(e, location, query)).notNull().onlyOrNull();

  if (isCompatibleWithUrl(r, location, query))
    return r;

  return null;
}

export default function ToolbarRenderer(p: {
  onAutoClose?: () => void | undefined;
  appTitle: React.ReactNode
}): React.ReactElement | null {
  const response = useAPI(() => ToolbarClient.API.getCurrentToolbar("Side"), []);
  const responseRef = useUpdatedRef(response);

  const [refresh, setRefresh] = React.useState(false);
  const [active, setActive] = React.useState<ToolbarClient.ToolbarResponse<any> | null>(null);
  const activeRef = useUpdatedRef(active);

  function changeActive(location: History.Location) {
    var query = QueryString.parse(location.search);
    if (responseRef.current) {
      if (activeRef.current && isCompatibleWithUrl(activeRef.current, location, query)) {
        return;
      }

      var newActive = inferActive(responseRef.current, location, query);
      setActive(newActive);
    }
  }

  useHistoryListen((location: History.Location, action: History.Action) => {
    changeActive(location);
  }, response != null);

  React.useEffect(() => changeActive(AppContext.history.location), [response]);

  return (
    <div className={"sidebar-inner"}>
      {p.appTitle}
      <div className={"close-sidebar"}
        onClick={() => p.onAutoClose && p.onAutoClose()}>
        <FontAwesomeIcon icon={"angle-double-left"} />
      </div>

      <div onClick={(ev) => {
        if ((ev.target as any).className != "nav-item-dropdown-elem") {
          p.onAutoClose && p.onAutoClose();
        }
      }}>
        {response && response.elements && response.elements.map((res: ToolbarClient.ToolbarResponse<any>, i: number) => renderNavItem(res, () => setTimeout(() => setRefresh(!refresh), 500), i))}
      </div>
    </div>
  );

  function renderNavItem(res: ToolbarClient.ToolbarResponse<any>, onRefresh: () => void, key: string | number) {

    switch (res.type) {
      case "Divider":
        return <hr style={{ margin: "10px 0 5px 0px" }} key={key}></hr>;
      case "Header":
      case "Item":
        if (res.elements && res.elements.length) {
          var title = res.label || res.content!.toStr;
          var icon = ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor);

          return (
            <ToolbarDropdown parentTitle={title} icon={icon} key={key}>
              {res.elements && res.elements.map((sr, i) => renderNavItem(sr, onRefresh, i))}
            </ToolbarDropdown>
          );
        }

        if (res.url) {
          return (
            <ToolbarNavItem key={key} title={res.label} onClick={(e: React.MouseEvent<any>) => AppContext.pushOrOpenInTab(res.url!, e)}
            active={res == active} icon={ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor)} />
          );
        }

        if (res.content) {
          var config = ToolbarClient.getConfig(res);
          if (!config)
            return <Nav.Item style={{ color: "red" }}>{res.content!.EntityType + "ToolbarConfig not registered"}</Nav.Item>;

          return config.getMenuItem(res, res == active, key);
        }

        if (res.type == "Header") {
          return (
            <div key={key} className={"nav-item-header"}>
              {ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor)}
              <span className={"nav-item-text"}>{res.label}</span>
              <div className={"nav-item-float"}>{res.label}</div>
            </div>
          );
        }

        return <Nav.Item key={key} style={{ color: "red" }}>{"No Content or Url found"}</Nav.Item>;

      default:
        throw new Error("Unexpected " + res.type);
    }
  }
}

function ToolbarDropdown(props: { parentTitle: string | undefined, icon: any, children: any }) {
  var [show, setShow] = React.useState(false);

  return (
    <div>
      <ToolbarNavItem title={props.parentTitle} onClick={() => setShow(!show)}
        icon={
          <div style={{ display: 'inline-block', position: 'relative' }}>
            <div className="nav-arrow-icon" style={{ position: 'absolute' }}><FontAwesomeIcon icon={show ? "caret-down" : "caret-right"} className="icon" /></div>
            <div className="nav-icon-with-arrow">
              {props.icon}
            </div>
          </div>
        }
      />
      <div style={{ display: show ? "block" : "none" }} className="nav-item-sub-menu">
        {show && props.children}
      </div>
    </div>
  );
}

export function ToolbarNavItem(p: { title: string | undefined, active?: boolean, onClick: (e: React.MouseEvent) => void, icon?: React.ReactNode }) {
  return (
    <Nav.Item >
      <Nav.Link title={p.title} onClick={p.onClick} active={p.active}>
        {p.icon} 
        <span className={"nav-item-text"}>{p.title}</span>
        <div className={"nav-item-float"}>{p.title}</div>
      </Nav.Link>
    </Nav.Item >
  );
}
