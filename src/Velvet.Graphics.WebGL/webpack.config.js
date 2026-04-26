const path = require('path');

module.exports = {
  entry: './ts/interop/index.ts',
  output: {
    filename: 'velvet.js',
    path: path.resolve(__dirname, 'wwwroot'),
    library: "VelvetModule",
    libraryTarget: "umd"
  },
  resolve: {
    extensions: ['.ts', '.js'],
  },
  module: {
    rules: [
      {
        test: /\.(glsl|vs|fs|vert|frag)$/i,
        type: "asset/source",
        include: [
          path.resolve(__dirname, 'ts/shaders'),
          path.resolve(__dirname, 'wwwroot/shaders')
        ]
      },
      {
        test: /\.ts$/,
        use: "ts-loader",
        exclude: /node_modules/,
      }
    ]
  },
  mode: 'production',
};
