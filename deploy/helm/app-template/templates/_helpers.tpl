{{/*
Naming helpers. Every resource in this chart is named
  <appKey>-<environment>[-<workload>]
so several environments can share a namespace without colliding.

Names alone are not enough for that promise: Services, PodDisruptionBudgets and
workload selectors match on LABELS, not names. See `app-template.selectorLabels`
below — it is the single source of truth for both pod labels and every selector,
and it is what actually keeps dev and prd apart in a shared namespace.
*/}}

{{/* Base name: `appKey` if set, otherwise the chart name. */}}
{{- define "app-template.name" -}}
{{- default .Chart.Name .Values.appKey | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/* Release-wide prefix, e.g. apptemplate-prd */}}
{{- define "app-template.fullname" -}}
{{- printf "%s-%s" (include "app-template.name" .) .Values.environment | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Per-workload resource name, e.g. apptemplate-prd-api.
Called with a two-item list: (list $root $workloadName)
*/}}
{{- define "app-template.serviceName" -}}
{{- $root := index . 0 -}}
{{- $name := index . 1 -}}
{{- printf "%s-%s" (include "app-template.fullname" $root) $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Selector labels — the identity every pod carries and every Service /
PodDisruptionBudget / workload selector matches on.

`app.kubernetes.io/instance` is what makes the "several environments in one
namespace" promise above actually hold: without it a dev Service would happily
select prd pods, because name + component are identical across environments.
Set it to the release-wide prefix (<appKey>-<environment>), NOT to .Release.Name,
so it stays stable no matter what the operator called `helm install`.

Called with a two-item list: (list $root $workloadName)

NOTE: `spec.selector` on a Deployment and `spec.selector` on a PDB are
immutable. Adding this label to a chart that is ALREADY installed makes
`helm upgrade` fail with "field is immutable" — delete the release
(`helm uninstall`) and install it again, or roll the workloads under new names.
*/}}
{{- define "app-template.selectorLabels" -}}
{{- $root := index . 0 -}}
{{- $name := index . 1 -}}
app.kubernetes.io/name: {{ include "app-template.name" $root }}
app.kubernetes.io/instance: {{ include "app-template.fullname" $root }}
app.kubernetes.io/component: {{ $name }}
{{- end -}}

{{/*
URL prefix the app is served under.
  hostingMode: path -> "/<pathPrefix>"   (shared hostname, one path per app)
  hostingMode: host -> ""                (the app owns the hostname)
*/}}
{{- define "app-template.basePath" -}}
{{- if eq .Values.hostingMode "path" -}}
/{{ .Values.pathPrefix | trimPrefix "/" | trimSuffix "/" }}
{{- else -}}
{{- "" -}}
{{- end -}}
{{- end -}}
