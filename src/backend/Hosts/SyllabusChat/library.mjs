import { createHash } from "node:crypto";

// Written against the supplied PDF, not generated at request time. Changes to
// the document require reviewing these answers before updating the fingerprint.
const syllabusHash = "481b65a2d8c63cfd5c37c5ada935626087fad93d4b74664c734db9fb8c3ca1ac";
const citation = (pdfPage, evidence) => ({ pdfPage, evidence });
const entry = (id, question, aliases, answer, citations) => ({ id, question, aliases, answer, citations });

export const entries = [
  entry("kicking", "When do students learn kicking?", ["kicking", "kick a ball", "what are the Primary 2 kicking outcomes", "how to teach kicking", "kicking progression", "which year is kicking introduced"],
    "Based on the 2024 PE syllabus, kicking is learnt in Primary 2 under Games and Sports. Students learn to kick a stationary ball with the instep towards a wall at least 6 metres away, then kick a stationary ball using a smooth running approach.",
    [citation(37, "PRIMARY 2 – GAMES AND SPORTS"), citation(37, "Kick using the instep of the foot a ball from a stationary position to a wall, at least 6 m away."), citation(37, "Kick a stationary ball using a smooth running approach.")]),
  entry("net-barrier", "When is 3v3 net-barrier taught?", ["3 v 3 net and barrier", "three versus three net barrier", "when do students learn net barrier games", "3v3 net barrier learning outcomes", "what is taught in 3v3 net barrier"],
    "3v3 net-barrier games are included in the Primary 5 and 6 learning outcomes. Students work on setting up an attack and defending space. The syllabus groups Primary 5 and 6 together; it does not assign 3v3 exclusively to one of those years.",
    [citation(40, "P RIMARY 5 AND 6: L EARNING O UTCOMES - N ET -B ARRIER C ATEGORY"), citation(44, "3v3 Shot placement to own side (Object received middle, back half of court)")]),
  entry("net-barrier-rules", "What are the touch rules for 3v3 net-barrier?", ["3v3 net barrier touches", "how many touches in 3v3 net barrier", "can players have consecutive touches in net barrier"],
    "In the listed 3v3 net-barrier situations, each team is allowed a specified number of touches before sending the object over the net, and consecutive touches by the same player are not allowed. The syllabus uses ‘X number of touches’, rather than prescribing one fixed number for all these situations.",
    [citation(46, "3v3 Shot placement to own side (Object received front half of court)"), citation(46, "Each team allowed X number of touches before object is sent over the net"), citation(46, "No consecutive touches allowed")]),
  entry("landing", "How can I teach safe landing?", ["safe landing technique", "how should students land in gymnastics", "controlled landing", "landing cues"],
    "For a controlled landing in gymnastics, students contact the surface with the balls of their feet first, then bend their ankles, knees and hips to cushion the landing. They tighten their abdominal muscles and keep their arms outstretched for balance. This is the syllabus technique guidance, not a complete lesson-specific safety assessment.",
    [citation(77, "Landing in a controlled finish position requires the student to contact the landing surface first with the balls of the feet"), citation(77, "to cushion the landing by bending at the ankles, knees and hips, and to control the landing by tightening the abdominal muscles and keeping the arms outstretched for balance.")]),
  entry("athletics", "When is athletics introduced?", ["athletics", "when do students start athletics", "which primary year teaches athletics"],
    "Athletics is introduced at Primary 4 in the 2024 PE syllabus.",
    [citation(18, "Athletics is introduced at Primary 4 and Swimming is to be completed by Primary 6.")]),
  entry("swimming", "When should swimming be completed?", ["swimming", "when do students learn swimming", "which primary year is swimming taught", "when does swimming start"],
    "Swimming is to be completed by Primary 6. The syllabus states an end-of-primary completion requirement rather than assigning swimming to one fixed starting year.",
    [citation(18, "The developmentally appropriate Learning Outcomes (LOs) specify minimally what students should know and be able to do for all the learning areas by each specific level except for swimming which is to be completed by the end of the primary level.")]),
  entry("throwing", "When do students learn throwing and catching?", ["throwing", "catching", "throw and catch", "Primary 1 throwing catching outcomes"],
    "Throwing and catching are included in Primary 1 Games and Sports. Outcomes include underhand, two-handed overhead and overhand throws, alongside catching self-tossed, bounced and gently thrown balls.",
    [citation(36, "PRIMARY 1 – GAMES AND SPORTS"), citation(36, "Throw using the underhand movement pattern, a variety of small objects towards a large target at least 3 metres away, at a low and medium level."), citation(36, "Catch using two hands a gently thrown ball from 3 metres away, at waist level.")]),
  entry("dance", "When is dance introduced?", ["dance", "when do students learn dancing"],
    "Dance is introduced at Primary 1, alongside Games and Sports and Gymnastics.",
    [citation(18, "Most content areas for Physical Activity (i.e., Dance, Games and Sports, and Gymnastics), Outdoor Education, and Physical Health and Safety are introduced at Primary 1.")]),
  entry("gymnastics", "When is gymnastics introduced?", ["gymnastics", "when do students start gymnastics"],
    "Gymnastics is introduced at Primary 1 in the 2024 PE syllabus.",
    [citation(18, "Most content areas for Physical Activity (i.e., Dance, Games and Sports, and Gymnastics), Outdoor Education, and Physical Health and Safety are introduced at Primary 1.")]),
  entry("outdoor", "When is outdoor education introduced?", ["outdoor education", "outdoor learning introduction"],
    "Outdoor Education is introduced at Primary 1. Its primary-level strands are Outdoor Living, Sense of Place, and Risk Assessment and Management.",
    [citation(18, "Outdoor Education • Outdoor Living • Sense of Place • Risk Assessment and Management"), citation(18, "Outdoor Education, and Physical Health and Safety are introduced at Primary 1.")]),
  entry("fms", "What are fundamental movement skills (FMS)?", ["where can I find FMS guidance", "fundamental motor skills", "FMS categories", "motor skills framework"],
    "The syllabus groups fundamental motor skills into locomotor, non-locomotor and manipulative skills. Examples include running and jumping; balancing and twisting; and throwing, catching and kicking. These are set out in the Motor Skills and Concepts Framework.",
    [citation(20, "promote students’ comp etence in a variety of locomotor, non-locomotor and manipulative skills")]),
  entry("concepts", "What are the four movement concepts?", ["movement concepts", "body awareness space awareness effort relationships"],
    "The four movement concepts are body awareness (what the body is doing), space awareness (where movement occurs), effort (how the body moves), and relationships (with whom or what it moves).",
    [citation(20, "body awareness (what the body is doing)"), citation(20, "space awareness (where the body and object are moving)"), citation(20, "effort (how the body is moving)"), citation(20, "relationships (with whom or what the body is relating to as it moves)")]),
  entry("areas", "What are the primary PE learning areas?", ["primary learning areas", "primary curriculum overview", "what does primary PE cover"],
    "The three primary PE learning areas are Physical Activity, Outdoor Education, and Physical Health and Safety. Physical Activity covers Athletics, Dance, Games and Sports, Gymnastics, and Swimming.",
    [citation(18, "Physical Activity • Athletics • Dance • Games and Sports • Gymnastics • Swimming"), citation(18, "Outdoor Education • Outdoor Living"), citation(18, "Physical Health and Safety • Physical Fitness")]),
  entry("camp", "When is the outdoor adventure camp held?", ["outdoor camp", "camp duration", "how long is the camp", "when do students go camping"],
    "Students should experience a 3-day, 2-night outdoor adventure learning cohort camp by the end of Primary 5.",
    [citation(19, "a 3-Day 2-Night outdoor adventure learning cohort camp by the end of Primary 5")]),
  entry("trip", "When should students have a neighbourhood day trip?", ["neighbourhood trip", "neighborhood day trip", "outdoor day trip"],
    "Students should experience a day trip in the school’s neighbourhood by the end of Primary 4.",
    [citation(19, "a day trip in the school’s neighbourhood by the end of Primary 4")]),
  entry("events", "How many competitions or performances should students experience?", ["recreational competitions performances", "culminating competitions", "competitions by Primary 6"],
    "Students should experience at least two recreational competitions or performances by the end of Primary 6.",
    [citation(19, "at least 2 recreational competitions or performances by the end of Primary 6")]),
  entry("practice", "How many lessons should a learning outcome take?", ["learning outcome lesson duration", "how much practice is needed", "can outcomes be achieved in one or two lessons"],
    "Some learning outcomes can be achieved in one or two 30-minute lessons, but not all. The syllabus calls for sequences of lessons and distributed practice throughout the year so students develop control and precision.",
    [citation(19, "Some of the LOs can be achieved through 1 or 2 lessons (each at 30 minutes). However, not all LOs are meant to be achieved within 1 to 2 lessons."), citation(19, "These LOs can be achieved through a sequence of lessons and distributed practice throughout the school year.")]),
  entry("health", "When do students work on a personal health practice?", ["health practice personal improvement", "personal health practice years"],
    "Students work on a health practice for personal improvement in each year from Primary 3 to Primary 6.",
    [citation(19, "work on a health practice for personal improvement in each year from Primary 3 to 6.")]),
];

const normalise = text => text.toLowerCase().replace(/\bthree\s*(?:versus|vs|v|on)\s*three\b/g, "3v3")
  .replace(/\b(\d)\s*(?:versus|vs\.?|v|on)\s*(\d)\b/g, "$1v$2")
  .replace(/\bp([1-6])\b/g, "primary $1").replace(/[^a-z0-9]+/g, " ").trim();
const ignored = new Set("a an the is are was be been do does did i we you my our can could would should please tell me about based on according to in of at for and or with from this that syllabus 2024 pe students student pupils pupil children child learn learns learning learnt learned teach teaches taught teaching introduced introduce introduction start starts when which what how year years level levels".split(" "));
const synonyms = { kick: "kicking", throw: "throwing", catch: "catching", dancing: "dance", games: "game", skills: "skill", outcomes: "outcome", lessons: "lesson", categories: "category", references: "source" };
const tokens = text => [...new Set(normalise(text).split(" ").filter(word => word && !ignored.has(word)).map(word => synonyms[word] ?? word))];
const subjects = {
  kicking: ["kicking"], "net-barrier": ["net", "barrier"], "net-barrier-rules": ["net", "barrier"],
  landing: ["landing", "land"], athletics: ["athletics"], swimming: ["swimming"], throwing: ["throwing", "catching"],
  dance: ["dance"], gymnastics: ["gymnastics"], outdoor: ["outdoor"], fms: ["fms", "motor", "fundamental"],
  concepts: ["concepts", "awareness", "effort", "relationships"], areas: ["areas", "curriculum", "cover"],
  camp: ["camp", "camping"], trip: ["trip"], events: ["competitions", "performances"],
  practice: ["lesson", "practice"], health: ["health"],
};

export function createLibraryAnswerer(pages) {
  if (createHash("sha256").update(pages.map(page => page.text).join("\f")).digest("hex") !== syllabusHash) {
    throw new Error("Review the syllabus answer library after changing the PDF.");
  }
  const checked = entries.map(item => ({
    ...item,
    vocabulary: new Set([item.question, ...item.aliases].flatMap(tokens)),
    citations: item.citations.map(reference => {
      const page = pages.find(page => page.pageNumber === reference.pdfPage);
      if (!page || !page.text.replace(/\s+/g, " ").includes(reference.evidence)) throw new Error(`Invalid library evidence: ${item.id}`);
      return { ...reference, printedPage: page.printedPage };
    }),
  }));
  function match(question) {
    const query = tokens(question);
    if (!query.length) return undefined;
    // All meaningful words must be covered. Do not answer a different question
    // just because it contains a familiar topic (e.g. "swimming kicking").
    const candidates = checked.filter(item => query.some(word => subjects[item.id].includes(word))
      && query.every(word => item.vocabulary.has(word)))
      .map(item => ({ item, score: Math.max(...[item.question, ...item.aliases].map(alias => {
        const words = tokens(alias);
        return query.every(word => words.includes(word)) ? query.length / words.length : 0;
      })) })).sort((a, b) => b.score - a.score);
    if (!candidates.length || (candidates[1] && candidates[0].score === candidates[1].score)) return undefined;
    const best = candidates[0].item;
    // A technique/definition answer is not evidence for a starting year.
    if (/\b(when|year|level)\b/.test(normalise(question)) && ["landing", "fms", "concepts", "areas", "net-barrier-rules", "practice"].includes(best.id)) return undefined;
    return best;
  }
  const topics = entries.map(({ id, question }) => ({ id, question }));
  return {
    configured: false, available: true, mode: "syllabus-library", topics,
    async answer(question, history = []) {
      const followup = /^(tell me more|more details|where is that|source|show (me )?(the )?(source|evidence)|what page|which page)$/.test(normalise(question));
      const previous = [...history].reverse().find(message => message.role === "user");
      const found = match(followup && previous ? previous.content : question);
      if (!found) return {
        supported: false, mode: "syllabus-library", citations: [],
        answer: "I don’t have a prepared answer for that question in this syllabus library. That does not mean the syllabus has no guidance on it. Please choose a question below or browse the available questions; you can also open the full PDF.",
        suggestions: topics.slice(0, 3).map(topic => topic.question),
      };
      return { supported: true, mode: "syllabus-library", answer: found.answer, citations: found.citations, suggestions: [] };
    },
  };
}
