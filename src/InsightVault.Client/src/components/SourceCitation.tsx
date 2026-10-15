import type { SourceCitationDto } from '../api/chat'
export function SourceCitation({ source }: { source: SourceCitationDto }) {
  return <article className="source-card"><header><span className="source-number">{source.rank}</span><div><h3>{source.documentName}</h3><p>Version {source.documentVersion}{source.sourcePageNumber > 0 ? ' · Page ' + source.sourcePageNumber : ''}{source.sectionTitle ? ' · ' + source.sectionTitle : ''}</p></div></header><p className="source-excerpt">{source.text}</p></article>
}
