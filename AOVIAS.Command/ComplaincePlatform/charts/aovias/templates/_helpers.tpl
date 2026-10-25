{{- define "aovias.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "aovias.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- include "aovias.name" . -}}
{{- end -}}
{{- end -}}

{{- define "aovias.labels" -}}
app.kubernetes.io/name: {{ include "aovias.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version }}
{{- end -}}

{{- define "aovias.selectorLabels" -}}
app.kubernetes.io/name: {{ include "aovias.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end -}}
