# EvidenceLinks are explicit confirmed entities, not automatic tag-matching

Demand could have been computed from shared tags: if a StudyItem and a JobAnalysis share the tag "kafka", demand rises automatically. We rejected tag-matching because it is opaque (many weak signals accumulate invisibly), hard to audit (why does this item have demand 4.2?), and couples the demand signal to a normalisation convention rather than to a deliberate user act.

Instead, EvidenceLinks are explicit entities created only from AI-proposed, human-confirmed LinkProposals. Each link carries a weight and a rationale visible in the UI. Demand is the capped sum of confirmed link weights, fully traceable to specific evidence decisions. Tags remain in the model for organisation and filtering, not for demand computation.
