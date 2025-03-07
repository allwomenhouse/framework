
import * as React from 'react'
import { ValueLine, EntityRepeater, EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { LinkListPartEntity, LinkElementEmbedded } from '../Signum.Entities.Dashboard'

export default function SearchValuePart(p : { ctx: TypeContext<LinkListPartEntity> }){
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  return (
    <div>
      <EntityTable ctx={ctx.subCtx(p => p.links)} />
    </div>
  );
}
