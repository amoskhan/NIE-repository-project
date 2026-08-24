{{- define "application.name" -}}
{{- default .Chart.Name .Values.appKey | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "application.fullname" -}}
{{- printf "%s-%s" (include "application.name" .) .Values.environment | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "application.serviceName" -}}
{{- $root := index . 0 -}}
{{- $name := index . 1 -}}
{{- printf "%s-%s" (include "application.fullname" $root) $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "application.basePath" -}}
{{- if eq .Values.hostingMode "path" -}}
/{{ .Values.pathPrefix | trimPrefix "/" | trimSuffix "/" }}
{{- else -}}
{{- "" -}}
{{- end -}}
{{- end -}}
