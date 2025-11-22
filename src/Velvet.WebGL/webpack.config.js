const path = require('path');

module.exports = {
  entry: './ts/index.ts',
  output: {
    filename: 'velvet.js',
    path: path.resolve(__dirname, 'dist'),
    library: {
      name: 'Velvet',
      type: 'umd',
      export: 'default',
    },
    globalObject: 'this',
  },
  resolve: {
    extensions: ['.ts', '.js'],
  },
  module: {
    rules: [
      {
        test: /\.ts$/,
        use: 'ts-loader',
        exclude: /node_modules/,
      },
      {
        test: /\.(vert|frag)$/,
        type: 'asset/source',
      },
    ],
  },
  mode: 'production',
};
