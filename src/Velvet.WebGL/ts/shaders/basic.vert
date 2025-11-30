#version 100
attribute vec2 position;
attribute vec3 color;
varying vec3 vColor;

uniform mat4 uModel;

void main() {
    vColor = color;
    gl_Position = uModel * vec4(position, 0.0, 1.0);
}
